using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace CineAboutApp.Interfaces.General
{
    public interface ITokenHandler<T>
        where T : class
    {
        string Create(T user);
        string CreateRefreshToken(int size = 64);
        string CreateResetToken(int size = 32);
        JwtSecurityToken GetDecodedJwt(string token, bool validateSignature = false);
        JwtSecurityToken GetDecodedJwt(HttpRequest request, bool validateSignature = false);
        public string GetJwtSub(HttpRequest request);
        public string GetJwtSub(string request);
        string GetJwtClaimValue(HttpRequest request, string claimName);
        string GetJwtClaimValue(string token, string claimName);
        string GetTokenFromRequest(HttpRequest request);
        bool IsAdmin(string token);
        bool AllowedToAccessEvent(string token, int eventId, string[] allowedRoles);
    }
}
