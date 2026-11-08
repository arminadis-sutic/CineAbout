using CineAboutApp.Interfaces.General;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace CineAboutApp.WebAPI.Middleware
{
    public class TokenBlacklistMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            string? token = null;

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }
            else if (context.Request.Query.ContainsKey("access_token"))
            {
                token = context.Request.Query["access_token"];
            }

            var jti = string.IsNullOrEmpty(token) ? null : TryReadJti(token);

            if (!string.IsNullOrEmpty(jti) && await blacklistService.IsTokenBlacklistedAsync(jti))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Token has been revoked.");
                return;
            }

            await next(context);
        }

        private static string? TryReadJti(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return null;
            }

            try
            {
                return handler.ReadJwtToken(token).Claims
                    .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            }
            catch (Exception ex) when (ex is ArgumentException or SecurityTokenException or FormatException)
            {
                return null;
            }
        }
    }
}
