using CineAboutApp.Models.Enums;

namespace CineAboutApp.Services.Database
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalTitle { get; set; } = string.Empty;
        public MovieType Type { get; set; }
        public int ReleaseYear { get; set; }
        public int Duration { get; set; }
        public string Synopsis { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public bool IsVisible { get; set; }

        public int LanguageId { get; set; }
        public Language Language { get; set; } = null!;

        public int StudioId { get; set; }
        public Studio Studio { get; set; } = null!;

        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
        public ICollection<MovieCast> MovieCasts { get; set; } = new List<MovieCast>();
        public ICollection<Award> Awards { get; set; } = new List<Award>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();
        public ICollection<SwipeHistory> SwipeHistories { get; set; } = new List<SwipeHistory>();
        public ICollection<CineAboutApp.Services.Database.Thread> Threads { get; set; } = new List<CineAboutApp.Services.Database.Thread>();
    }
}
