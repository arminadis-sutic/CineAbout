using CineAboutApp.Interfaces.General;
using CineAboutApp.Models.Mail;
using CineAboutApp.Models.Messages;
using EasyNetQ;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

DotNetEnv.Env.Load();

using var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger("SubscriberProgram");
logger.LogInformation("CineAbout Subscriber Service starting...");

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var deliveryMode = (config["DELIVERY_MODE"] ?? "live").Trim().ToLowerInvariant();
if (deliveryMode is not ("live" or "test"))
    throw new InvalidOperationException("DELIVERY_MODE must be 'live' or 'test'.");
var testMode = deliveryMode == "test";
if (testMode)
    logger.LogWarning("DELIVERY MODE = TEST: e-mails and push notifications are NOT sent (messages are consumed and logged only).");

FirebaseApp? firebaseApp = null;
var firebaseJson = testMode ? string.Empty : Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON") ?? string.Empty;

try
{
    var json = firebaseJson.Trim();
    if (json.Length >= 2 && json.StartsWith('"') && json.EndsWith('"')) json = json[1..^1];
    json = json.Replace("\\\\n", "\\n");

    if (!string.IsNullOrWhiteSpace(json) && !json.Contains("CHANGE_ME_IN_ENV", StringComparison.OrdinalIgnoreCase))
    {
        using var document = JsonDocument.Parse(json);
        if (json.Contains("\"private_key\"", StringComparison.Ordinal))
        {
            firebaseApp = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(json),
                ProjectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
            });
            logger.LogInformation("Firebase initialized from FIREBASE_CREDENTIALS_JSON (project {Project}).", Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID"));
        }
        else
        {
            logger.LogWarning("FIREBASE_CREDENTIALS_JSON has no private key. Push notifications are disabled.");
        }
    }
}
catch (Exception ex)
{
    logger.LogError("Firebase initialization from FIREBASE_CREDENTIALS_JSON failed ({Reason}). Push notifications are disabled.", ex.GetType().Name);
}

try
{
    var firebaseFile = testMode ? null : Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_FILE");
    if (firebaseApp == null && !string.IsNullOrWhiteSpace(firebaseFile) && File.Exists(firebaseFile))
    {
        if (File.ReadAllText(firebaseFile).Contains("\"private_key\"", StringComparison.Ordinal))
        {
            firebaseApp = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(firebaseFile),
                ProjectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
            });
            logger.LogInformation("Firebase initialized from the mounted credentials file (project {Project}).", Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID"));
        }
    }

    if (firebaseApp == null && !testMode)
        logger.LogWarning("Firebase credentials are missing or have no private key. Push notifications are disabled.");
}
catch (Exception ex)
{
    logger.LogError("Firebase initialization from the credentials file failed ({Reason}). Push notifications are disabled.", ex.GetType().Name);
}

var rabbitMqConnection = config.GetConnectionString("RabbitMQ");
if (string.IsNullOrWhiteSpace(rabbitMqConnection))
    throw new InvalidOperationException("RabbitMQ connection string is missing.");

logger.LogInformation("Connecting to RabbitMQ...");
using var bus = RabbitHutch.CreateBus(rabbitMqConnection);
logger.LogInformation("Connected to RabbitMQ successfully.");

var mailSettings = Options.Create(new MailSettings
{
    Host = config["MailSettings:Host"] ?? string.Empty,
    Port = config.GetValue<int>("MailSettings:Port"),
    EnableSsl = config.GetValue<bool>("MailSettings:EnableSsl"),
    UseDefaultCredentials = config.GetValue<bool>("MailSettings:UseDefaultCredentials"),
    Email = config["MailSettings:Email"] ?? string.Empty,
    Password = (config["MailSettings:Password"] ?? string.Empty).Replace(" ", string.Empty),
    Username = config["MailSettings:Username"] ?? string.Empty
});

var mailService = new SubscriberMailService(mailSettings);

await bus.PubSub.SubscribeAsync<Email>("emailSubscriber", async data =>
{
    if (testMode)
    {
        logger.LogInformation("[TEST MODE] e-mail with subject {Subject} was NOT sent.", data.MailData.Subject);
        return;
    }

    if (!mailService.IsConfigured)
    {
        logger.LogWarning("SMTP is not configured (MAIL_HOST / MAIL_EMAIL / MAIL_PASSWORD); the e-mail with subject {Subject} was not sent.", data.MailData.Subject);
        return;
    }

    await ExecuteWithRetryAsync(
        () => mailService.Send(data.MailData),
        "Email processing",
        logger);

    logger.LogInformation(
        "Email sent to {Recipient} with subject {Subject}",
        data.MailData.To,
        data.MailData.Subject);
});

await bus.PubSub.SubscribeAsync<PushNotification>("notificationSubscriber", async data =>
{
    await ExecuteWithRetryAsync(async () =>
    {
        if (testMode)
        {
            logger.LogInformation("[TEST MODE] push notification for {Count} token(s) was NOT sent.", data.Tokens?.Count ?? 0);
            return;
        }

        if (firebaseApp == null)
        {
            logger.LogInformation("Firebase is unavailable; push notification skipped.");
            return;
        }

        if (data.Tokens == null || data.Tokens.Count == 0)
        {
            logger.LogInformation("No push notification tokens were provided.");
            return;
        }

        var response = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(new MulticastMessage
            {
                Tokens = data.Tokens,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Priority = NotificationPriority.MAX,
                        Body = data.Body,
                        Title = data.Title,
                        ImageUrl = string.IsNullOrWhiteSpace(data.ImageUrl)
                            ? null
                            : data.ImageUrl,
                        ChannelId = "reminders",
                        Sound = "default"
                    }
                },
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = data.Title,
                    Body = data.Body,
                    ImageUrl = string.IsNullOrWhiteSpace(data.ImageUrl)
                        ? null
                        : data.ImageUrl
                },
                Data = data.Data
            });

        logger.LogInformation(
            "Push notifications sent: {SuccessCount}; failed: {FailureCount}",
            response.SuccessCount,
            response.FailureCount);
    }, "Push notification processing", logger);
});

logger.LogInformation("Subscriber service is running.");
logger.LogInformation("Email notifications: {Status}", testMode ? "TEST MODE (not sent)" : "enabled");
logger.LogInformation(
    "Push notifications: {Status}",
    testMode ? "TEST MODE (not sent)" : firebaseApp == null ? "disabled" : "enabled");
logger.LogInformation("Press Ctrl+C to stop.");

using var cancellationSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

try
{
    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationSource.Token);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Subscriber service is shutting down.");
}

static async Task ExecuteWithRetryAsync(
    Func<Task> action,
    string actionName,
    ILogger logger)
{
    var retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            5,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, delay, retryCount, _) =>
            {
                logger.LogWarning(
                    exception,
                    "Retry {RetryCount} for {ActionName} in {DelaySeconds} seconds.",
                    retryCount,
                    actionName,
                    delay.TotalSeconds);
            });

    await retryPolicy.ExecuteAsync(action);
}

public sealed class SubscriberMailService(IOptions<MailSettings> options) : IMailService
{
    private readonly MailSettings settings = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(settings.Host)
        && !string.IsNullOrWhiteSpace(settings.Email) && settings.Email.Contains('@')
        && !settings.Email.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(settings.Password);

    public async Task Send(MailData data)
    {
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            UseDefaultCredentials = settings.UseDefaultCredentials,
            Credentials = new NetworkCredential(settings.Email, settings.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(settings.Email, settings.Username),
            Subject = data.Subject,
            IsBodyHtml = data.IsBodyHtml,
            Priority = data.Priority,
            Body = data.Body.ToString()
        };

        message.To.Add(new MailAddress(data.To));

        foreach (var cc in data.CC ?? [])
            message.CC.Add(new MailAddress(cc));

        foreach (var bcc in data.BCC ?? [])
            message.Bcc.Add(new MailAddress(bcc));

        if (data.Attachments != null)
        {
            foreach (var attachment in data.Attachments)
            {
                if (attachment.Content == null || attachment.Content.Length == 0)
                    continue;

                var stream = new MemoryStream(attachment.Content);
                message.Attachments.Add(new Attachment(
                    stream,
                    attachment.FileName,
                    attachment.ContentType));
            }
        }

        await client.SendMailAsync(message);
    }
}
