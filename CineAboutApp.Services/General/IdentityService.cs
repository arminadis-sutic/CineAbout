using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CineAboutApp.Services.General
{
    public class IdentityService(IHttpContextAccessor httpContextAccessor) : IIdentityService
    {
        public int GetCurrentUserId()
        {
            var userId = GetCurrentUserIdNullable();
            if (!userId.HasValue)
                throw new UserException("Korisnik nije autentifikovan ili ID nedostaje.");
            
            return userId.Value;
        }

        public int? GetCurrentUserIdNullable()
        {
            var sub = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value 
                      ?? httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(sub) || !int.TryParse(sub, out var userId))
                return null;

            return userId;
        }

        public bool IsAdmin()
        {
            return IsInRole(AppRoles.Admin);
        }

        public bool IsAuthenticated()
        {
            return httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public IEnumerable<string> GetCurrentUserRoles()
        {
            var roles = httpContextAccessor.HttpContext?.User?.FindAll("role");
            return roles?.Select(r => r.Value) ?? Enumerable.Empty<string>();
        }

        public bool IsInRole(string roleName)
        {
            return httpContextAccessor.HttpContext?.User?.IsInRole(roleName) ?? false;
        }
    }
}
