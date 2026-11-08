using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Responses
{
    public class MovieCardResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public MovieType Type { get; set; }
        public int ReleaseYear { get; set; }
        public string PosterUrl { get; set; } = string.Empty;
        public List<string> Genres { get; set; } = new List<string>();
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class WatchlistItemResponse
    {
        public int Id { get; set; }
        public WatchlistType ListType { get; set; }
        public DateTime AddedAt { get; set; }
        public MovieCardResponse Movie { get; set; } = null!;
    }

    public class DiscoverCardResponse
    {
        public MovieCardResponse Movie { get; set; } = null!;
        public double Score { get; set; }
        public double GenreScore { get; set; }
        public double SimilarUsersScore { get; set; }
        public double PopularityScore { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
