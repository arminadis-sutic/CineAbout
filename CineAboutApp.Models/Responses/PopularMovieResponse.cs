namespace CineAboutApp.Models.Responses
{
    public class PopularMovieResponse
    {
        public int MovieId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;

        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }

        public int WatchlistCount { get; set; }
        public int InterestedCount { get; set; }

        public int PopularityScore { get; set; }
    }
}