using Microsoft.EntityFrameworkCore;

namespace CineAboutApp.Services.General
{
    public static class SeedLookupExtensions
    {
        public static async Task<Dictionary<string, T>> ToFirstWinsDictionaryAsync<T>(
            this IQueryable<T> source, Func<T, string> key, CancellationToken cancellationToken = default)
        {
            var rows = await source.ToListAsync(cancellationToken);
            var lookup = new Dictionary<string, T>();
            foreach (var row in rows)
            {
                lookup.TryAdd(key(row), row);
            }

            return lookup;
        }
    }
}
