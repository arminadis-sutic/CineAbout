using CineAboutApp.Interfaces.General;
using CineAboutApp.Models.Entities;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.General
{
    public class TokenBlacklistService(ApplicationDbContext db) : ITokenBlacklistService
    {
        public async Task BlacklistTokenAsync(string jti, DateTime expiryTime, int? userId = null)
        {
            if (await IsTokenBlacklistedAsync(jti)) return;

            db.BlacklistedTokens.Add(new BlacklistedToken
            {
                Jti = jti,
                ExpiryTime = expiryTime,
                UserId = userId
            });
            await db.SaveChangesAsync();
        }

        public async Task<bool> IsTokenBlacklistedAsync(string jti)
        {
            return await db.BlacklistedTokens.AnyAsync(t => t.Jti == jti);
        }
    }
}
