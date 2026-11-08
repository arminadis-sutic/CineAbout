using CineAboutApp.Model;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace CineAboutApp.WebAPI.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var errorKey = "serverError";
            var errorMessage = "Došlo je do greške na serveru. Molimo pokušajte ponovo.";

            switch (exception)
            {
                case NotFoundException ex:
                    statusCode = (int)HttpStatusCode.NotFound;
                    errorKey = "notFound";
                    errorMessage = ex.Message;
                    break;

                case UnauthorizedAccessException ex:
                    statusCode = (int)HttpStatusCode.Forbidden;
                    errorKey = "forbidden";
                    errorMessage = ex.Message;
                    break;

                case UserException:
                case BusinessException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    errorKey = "userError";
                    errorMessage = exception.Message;
                    break;

                case DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("REFERENCE constraint", StringComparison.OrdinalIgnoreCase) == true:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    errorKey = "userError";
                    errorMessage = "Zapis se ne može obrisati jer ga koriste drugi zapisi. Prvo uklonite ili promijenite povezane zapise.";
                    break;

                case DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    errorKey = "userError";
                    errorMessage = "Zapis s istim nazivom već postoji.";
                    break;
            }

            if (statusCode == (int)HttpStatusCode.InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception for {Method} {Path} (user {UserId}, trace {TraceId}): {Message}",
                    context.Request.Method, context.Request.Path, context.User.FindFirst("sub")?.Value ?? context.User.Identity?.Name ?? "anonymous",
                    context.TraceIdentifier, exception.Message);
            }
            else
            {
                _logger.LogWarning("Request refused with {StatusCode} ({ExceptionType}) for {Method} {Path}: {Message}",
                    statusCode, exception.GetType().Name, context.Request.Method, context.Request.Path, exception.Message);
            }

            var result = JsonSerializer.Serialize(new
            {
                errors = new Dictionary<string, string[]>
                {
                    [errorKey] = new[] { errorMessage }
                }
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsync(result);
        }
    }
}
