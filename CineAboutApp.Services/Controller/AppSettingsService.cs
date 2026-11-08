using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using CineAboutApp.Models.Responses;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.Controller
{
    public class AppSettingsService(ApplicationDbContext db, IMapper mapper, ICacheService cacheService) : IAppSettingsService
    {
        private const string CacheKey = "app_settings";

        public async Task<AppSettingsResponse> SetAppSettings(AppSettingsResponse appSettings, CancellationToken cancellationToken = default)
        {
            var settings = await db.AppSettings.FirstOrDefaultAsync(cancellationToken)
                ?? throw new UserException("No app settings found? Did you forget to seed data?");

            settings = mapper.Map(appSettings, settings);
            await db.SaveChangesAsync(cancellationToken);

            var response = mapper.Map<AppSettings, AppSettingsResponse>(settings);

            await cacheService.RemoveAsync(CacheKey);

            return response;
        }

        public async Task<AppSettingsResponse> GetAppSettingsAsync(CancellationToken cancellationToken = default)
        {
            var cached = await cacheService.GetAsync<AppSettingsResponse>(CacheKey);
            if (cached != null) return cached;

            var settings = await db.AppSettings.FirstOrDefaultAsync(cancellationToken)
                ?? throw new UserException("No app settings found? Did you forget to seed data?");

            var response = mapper.Map<AppSettingsResponse>(settings);
            await cacheService.SetAsync(CacheKey, response, ttlMinutes: 10);

            return response;
        }
    }
}
