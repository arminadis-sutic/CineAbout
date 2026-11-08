using PuppeteerSharp;
using PuppeteerSharp.Media;
using Scriban;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CineAboutApp.Services.General;

public class HtmlToPdfGenerator : IDisposable
{
    private static bool _browserDownloaded = false;
    private static readonly SemaphoreSlim _browserDownloadLock = new(1, 1);
    private static string? _executablePath;

    private static IBrowser? _browser;
    private static readonly SemaphoreSlim _browserLock = new(1, 1);

    private readonly ILogger<HtmlToPdfGenerator> _logger;

    public HtmlToPdfGenerator(ILogger<HtmlToPdfGenerator> logger)
    {
        _logger = logger;

        var envExecutable = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrEmpty(envExecutable) && File.Exists(envExecutable))
        {
            _executablePath = envExecutable;
            _browserDownloaded = true;
        }
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(
        string htmlTemplate,
        string? cssContent,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rendering Scriban template...");

        var template = Template.Parse(htmlTemplate);
        if (template.HasErrors)
        {
            _logger.LogError("Scriban template parse errors: {Errors}", string.Join(", ", template.Messages));
            throw new ArgumentException($"Template has errors: {string.Join(", ", template.Messages)}");
        }

        var renderedHtml = await template.RenderAsync(data);
        var fullHtml = BuildHtmlDocument(renderedHtml, cssContent);

        var browser = await GetBrowserAsync();
        _logger.LogInformation("Generating PDF...");

        await using var page = await browser.NewPageAsync();
        try
        {
            await page.SetContentAsync(fullHtml, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Load }
            });

            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "0",
                    Right = "0",
                    Bottom = "0",
                    Left = "0"
                }
            });

            _logger.LogInformation("PDF generated successfully, size: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static string BuildHtmlDocument(string bodyContent, string? cssContent)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='utf-8' />");
        if (!string.IsNullOrEmpty(cssContent))
        {
            sb.AppendLine($"<style>{cssContent}</style>");
        }
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine(bodyContent);
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        await EnsureBrowserDownloadedAsync();

        await _browserLock.WaitAsync();
        try
        {
            if (_browser == null || !_browser.IsConnected)
            {
                _logger.LogInformation("Launching Chromium browser instance...");
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = _executablePath,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-gpu", "--disable-dev-shm-usage" }
                });
                _logger.LogInformation("Chromium browser launched and reusable.");
            }

            return _browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }

    private async Task EnsureBrowserDownloadedAsync()
    {
        if (_browserDownloaded) return;

        await _browserDownloadLock.WaitAsync();
        try
        {
            if (_browserDownloaded) return;

            if (!string.IsNullOrEmpty(_executablePath))
            {
                _logger.LogInformation("Using pre-installed Chromium from environment variable: {Path}", _executablePath);
                return;
            }

            string[] commonPaths =
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                "/usr/bin/google-chrome",
                "/usr/bin/chromium",
                "/usr/bin/chromium-browser"
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogInformation("Found local browser at: {Path}", path);
                    _executablePath = path;
                    _browserDownloaded = true;
                    return;
                }
            }

            _logger.LogInformation("Downloading Chromium browser (first time only, ~280MB)...");
            var browserFetcher = new BrowserFetcher();
            var installedBrowser = await browserFetcher.DownloadAsync();
            _executablePath = installedBrowser.GetExecutablePath();
            _browserDownloaded = true;
            _logger.LogInformation("Chromium downloaded to: {Path}", _executablePath);
        }
        finally
        {
            _browserDownloadLock.Release();
        }
    }

    public static async Task InitializeBrowserAsync()
    {
        var tempLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<HtmlToPdfGenerator>();
        var generator = new HtmlToPdfGenerator(tempLogger);

        try
        {
            await generator.EnsureBrowserDownloadedAsync();
        }
        catch (Exception ex)
        {
            tempLogger.LogError(ex, "Failed to initialize Puppeteer browser during startup.");
        }
    }

    public void Dispose()
    {
        _browser?.Dispose();
        _browser = null;
    }
}
