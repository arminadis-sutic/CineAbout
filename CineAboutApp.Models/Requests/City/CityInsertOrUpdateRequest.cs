using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.City
{
    public class CityInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naziv grada je obavezan.")]
        [StringLength(200, ErrorMessage = "Naziv grada može imati najviše 200 znakova.")]
        public required string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Odaberite državu.")]
        public int CountryId { get; set; }
    }
}
