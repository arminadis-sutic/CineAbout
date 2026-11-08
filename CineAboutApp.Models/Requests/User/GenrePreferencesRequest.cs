using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.User
{
    public class GenrePreferencesRequest
    {
        [Required(ErrorMessage = "Lista žanrova je obavezna.")]
        [MaxLength(30, ErrorMessage = "Možete odabrati najviše 30 žanrova.")]
        public List<int> GenreIds { get; set; } = new List<int>();
    }
}
