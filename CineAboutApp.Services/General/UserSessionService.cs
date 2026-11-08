using CineAboutApp.Interfaces.General;
using CineAboutApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.General
{
    public class UserSessionService(ApplicationDbContext db) : IUserSessionService
    {
        public Task<bool> IsSessionValidAsync(int userId, int securityStamp, CancellationToken cancellationToken = default) =>
            db.MyAppUsers.AsNoTracking()
                .AnyAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted && u.SecurityStamp == securityStamp, cancellationToken);
    }
}
