namespace CineAboutApp.Interfaces.General
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, int? ttlMinutes = null);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
    }
}

