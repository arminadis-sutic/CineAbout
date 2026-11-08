namespace CineAboutApp.Interfaces.General
{
    public record TmdbTitle(int Id, string Title, string Overview, string PosterUrl, DateTime ReleaseDate);

    public interface ITmdbClient
    {
        Task<IReadOnlyList<TmdbTitle>> GetUpcomingAsync(CancellationToken cancellationToken = default);

        Task<TmdbTitle?> GetTitleAsync(int tmdbId, CancellationToken cancellationToken = default);
    }
}
