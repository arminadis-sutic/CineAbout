using CineAboutApp.Interfaces.General;
using Microsoft.AspNetCore.Authorization;

namespace CineAboutApp.WebAPI.Middleware
{
    public class SessionValidationMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, IUserSessionService sessions)
        {
            var needsCheck = context.User.Identity?.IsAuthenticated == true
                && context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() == null;

            if (needsCheck)
            {
                var userId = int.TryParse(context.User.FindFirst("sub")?.Value, out var id) ? id : 0;
                var stamp = int.TryParse(context.User.FindFirst("sv")?.Value, out var sv) ? sv : 0;

                if (userId == 0 || !await sessions.IsSessionValidAsync(userId, stamp, context.RequestAborted))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"errors\":{\"unauthorized\":[\"Sesija je istekla ili je poništena. Prijavite se ponovo.\"]}}");
                    return;
                }
            }

            await next(context);
        }
    }
}
