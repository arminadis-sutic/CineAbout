using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Requests.Watchlist
{
    public class WatchlistSetRequest
    {
        [System.ComponentModel.DataAnnotations.EnumDataType(typeof(WatchlistType), ErrorMessage = "Vrsta liste nije ispravna.")]
        public WatchlistType ListType { get; set; }
    }
}
