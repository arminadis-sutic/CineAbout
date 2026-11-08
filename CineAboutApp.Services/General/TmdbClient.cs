using CineAboutApp.Interfaces.General;
using CineAboutApp.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CineAboutApp.Services.General
{
    public class TmdbClient : ITmdbClient
    {
        public const string HttpClientName = "tmdb";

        private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w500";
        private const int UpcomingPages = 2;
        private static readonly TimeSpan UpcomingCacheDuration = TimeSpan.FromHours(6);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TmdbClient> _logger;
        private readonly string? _accessToken;

        public TmdbClient(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<TmdbClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
            _accessToken = configuration["Tmdb:AccessToken"];
        }

        public async Task<IReadOnlyList<TmdbTitle>> GetUpcomingAsync(CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue("tmdb_upcoming", out IReadOnlyList<TmdbTitle>? cached) && cached != null)
                return cached;

            var titles = new List<TmdbTitle>();
            for (var page = 1; page <= UpcomingPages; page++)
            {
                using var document = await GetJsonAsync($"movie/upcoming?language=en-US&page={page}", cancellationToken);
                if (document == null || !document.RootElement.TryGetProperty("results", out var results))
                    break;

                titles.AddRange(results.EnumerateArray().Select(ParseTitle).OfType<TmdbTitle>());
            }

            var unique = titles.GroupBy(t => t.Id).Select(g => g.First()).OrderBy(t => t.ReleaseDate).ToList();
            _cache.Set("tmdb_upcoming", (IReadOnlyList<TmdbTitle>)unique, UpcomingCacheDuration);
            return unique;
        }

        public async Task<TmdbTitle?> GetTitleAsync(int tmdbId, CancellationToken cancellationToken = default)
        {
            using var document = await GetJsonAsync($"movie/{tmdbId}?language=en-US", cancellationToken);
            return document == null ? null : ParseTitle(document.RootElement);
        }

        private async Task<JsonDocument?> GetJsonAsync(string relativeUrl, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_accessToken))
                throw new BusinessException("TMDB integracija nije konfigurisana (nedostaje TMDB_ACCESS_TOKEN).");

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            using var response = await client.SendAsync(request, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TMDB request {Url} failed with status {Status}", relativeUrl, (int)response.StatusCode);
                throw new BusinessException("Podaci o nadolazećim naslovima trenutno nisu dostupni. Pokušajte kasnije.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }

        private static TmdbTitle? ParseTitle(JsonElement element)
        {
            if (!element.TryGetProperty("id", out var id)
                || !element.TryGetProperty("release_date", out var releaseElement)
                || !DateTime.TryParseExact(releaseElement.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var release))
            {
                return null;
            }

            var poster = element.TryGetProperty("poster_path", out var posterElement) ? posterElement.GetString() : null;
            return new TmdbTitle(
                id.GetInt32(),
                element.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                element.TryGetProperty("overview", out var overview) ? overview.GetString() ?? string.Empty : string.Empty,
                string.IsNullOrEmpty(poster) ? string.Empty : ImageBaseUrl + poster,
                release);
        }
    }
}
