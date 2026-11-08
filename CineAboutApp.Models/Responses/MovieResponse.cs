using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Responses
{
    public class MovieResponse
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
        public string LanguageName { get; set; } = string.Empty;
        public int StudioId { get; set; }
        public string StudioName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int? DirectorId { get; set; }
        public string? DirectorName { get; set; }
        public WatchlistType? MyListType { get; set; }
        public List<MovieGenreResponse> Genres { get; set; } = new List<MovieGenreResponse>();
        public List<MovieCastResponse> Cast { get; set; } = new List<MovieCastResponse>();
    }

    public class MovieGenreResponse
    {
        public int GenreId { get; set; }
        public string GenreName { get; set; } = string.Empty;
    }

    public class MovieCastResponse
    {
        public int PersonId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public string RoleInMovie { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
    }
}
