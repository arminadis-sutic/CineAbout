using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using CineAboutApp.Services.Database;

namespace CineAboutApp.Services.General
{
    public class MyTokenHandler : ITokenHandler<MyAppUser>
    {
        private readonly string _secretKey;
        private readonly string _audience;
        private readonly string _issuer;
        private readonly int _expirationInHours;

        public MyTokenHandler(IConfiguration cfg)
        {
            _secretKey = cfg["Jwt:Secret"] ?? throw new ArgumentNullException("Jwt:Secret");
            _audience = cfg["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience");
            _issuer = cfg["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer");
            _expirationInHours = cfg.GetValue<int>("Jwt:ExpirationInHours");
            if (_expirationInHours <= 0) _expirationInHours = 24;
        }

        public string Create(MyAppUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new System.Security.Claims.ClaimsIdentity([
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
                    new Claim("Username", user.Username),
                    new Claim("sv", user.SecurityStamp.ToString()),
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                ]);

            foreach (var role in user.UserRoles.Where(ur => ur.IsActive))
            {
                claims.AddClaim(new Claim("role", role.Role!.Name));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Audience = _audience,
                Issuer = _issuer,
                Expires = DateTime.UtcNow.AddHours(_expirationInHours),
                SigningCredentials = credentials
            };

            var jwtHandler = new JsonWebTokenHandler();
            return jwtHandler.CreateToken(tokenDescriptor);
        }

        public string CreateRefreshToken(int size = 64)
        {
            byte[] bytes = new byte[size];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        public string CreateResetToken(int size = 32)
        {
            byte[] bytes = new byte[size];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public JwtSecurityToken GetDecodedJwt(HttpRequest request, bool validateSignature = true)
        {
            string token = request.Headers.Authorization.ToString().Replace("Bearer ", "");
            return GetDecodedJwt(token, validateSignature);
        }

        public JwtSecurityToken GetDecodedJwt(string token, bool validateSignature = true)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new UserException("JSON Web Token not found");
            }
            var handler = new JwtSecurityTokenHandler();

            if (validateSignature)
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = false
                };

                try
                {
                    handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                    return (JwtSecurityToken)validatedToken;
                }
                catch
                {
                    throw new UserException("Invalid JWT signature");
                }
            }

            return handler.ReadJwtToken(token);
        }

        public string GetJwtSub(HttpRequest request)
        {
            var decodedToken = GetDecodedJwt(request);
            return decodedToken.Claims.FirstOrDefault(x => x.Type == Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub)?.Value!;
        }

        public string GetJwtSub(string token)
        {
            var decodedToken = GetDecodedJwt(token);
            return decodedToken.Claims.FirstOrDefault(x => x.Type == Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames.Sub)?.Value!;
        }

        public string GetJwtRoleClaimValue(HttpRequest request)
        {
            var decodedToken = GetDecodedJwt(request);
            return decodedToken.Claims.FirstOrDefault(x => x.Type == "role")?.Value!;
        }
        public string GetJwtRoleClaimValue(string token)
        {
            var decodedToken = GetDecodedJwt(token);
            return decodedToken.Claims.FirstOrDefault(x => x.Type == "role")?.Value!;
        }

        public string GetJwtClaimValue(HttpRequest request, string claimName)
        {
            var decodedToken = GetDecodedJwt(request);
            return decodedToken.Claims.FirstOrDefault(x => x.Type == claimName)?.Value ?? string.Empty;
        }

        public string GetJwtClaimValue(string token, string claimName)
        {
            var decodedToken = GetDecodedJwt(token);
            return decodedToken.Claims.FirstOrDefault(x => x.Type == claimName)?.Value ?? string.Empty;
        }

        public bool IsAdmin(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }
            var decodedToken = GetDecodedJwt(token);
            return decodedToken.Claims.Where(c => c.Type == "role" && c.Value == AppRoles.Admin).Any();
        }

        public string GetTokenFromRequest(HttpRequest request)
        {
            string token = request.Headers.Authorization.ToString().Replace("Bearer ", "");
            return token;
        }

        public bool AllowedToAccessEvent(string token, int eventId, string[] allowedRoles)
        {
            return true;
        }
    }
}

