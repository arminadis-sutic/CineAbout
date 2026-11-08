using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Genre
{
    public class GenreInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naziv žanra je obavezan.")]
        [StringLength(200, ErrorMessage = "Naziv žanra može imati najviše 200 znakova.")]
        public required string Name { get; set; }
    }
}