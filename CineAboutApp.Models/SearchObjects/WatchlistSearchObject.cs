using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.SearchObjects
{
    public class WatchlistSearchObject : BaseSearchObject
    {
        public WatchlistType? ListType { get; set; }
        public MovieType? Type { get; set; }
    }
}
