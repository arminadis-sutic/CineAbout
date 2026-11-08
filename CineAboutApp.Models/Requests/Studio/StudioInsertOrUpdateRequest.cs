using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Studio
{
    public class StudioInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naziv studija je obavezan.")]
        [StringLength(200, ErrorMessage = "Naziv studija može imati najviše 200 znakova.")]
        public required string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Odaberite državu.")]
        public int CountryId { get; set; }

        [Range(1850, 2100, ErrorMessage = "Godina osnivanja mora biti između 1850. i 2100.")]
        public int FoundedYear { get; set; }

        [StringLength(2000, ErrorMessage = "Opis može imati najviše 2000 znakova.")]
        public string Description { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Adresa logotipa može imati najviše 500 znakova.")]
        public string LogoUrl { get; set; } = string.Empty;
    }
}
