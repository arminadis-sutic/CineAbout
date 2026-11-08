using CineAboutApp.Models.Responses;

namespace CineAboutApp.Interfaces.Controller
{
    public interface IAppSettingsService
    {
        public Task<AppSettingsResponse> SetAppSettings(AppSettingsResponse appSettings, CancellationToken cancellationToken = default);
        public Task<AppSettingsResponse> GetAppSettingsAsync(CancellationToken cancellationToken = default);
    }
}
