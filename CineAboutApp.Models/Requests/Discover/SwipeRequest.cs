using CineAboutApp.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Discover
{
    public class SwipeRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Film nije odabran.")]
        public int MovieId { get; set; }

        [System.ComponentModel.DataAnnotations.EnumDataType(typeof(SwipeDirection), ErrorMessage = "Smjer nije ispravan.")]
        public SwipeDirection Direction { get; set; }
    }
}
