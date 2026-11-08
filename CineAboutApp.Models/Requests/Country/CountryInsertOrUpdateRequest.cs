using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Country
{
    public class CountryInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naziv države je obavezan.")]
        [StringLength(200, ErrorMessage = "Naziv države može imati najviše 200 znakova.")]
        public required string Name { get; set; }
    }
}
