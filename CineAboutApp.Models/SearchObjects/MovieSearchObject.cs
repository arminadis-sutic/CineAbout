using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.SearchObjects
{
    public class MovieSearchObject : BaseSearchObject
    {
        public MovieType? Type { get; set; }
        public int? GenreId { get; set; }
        public int? ReleaseYear { get; set; }
    }
}
