using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Language
{
    public class LanguageInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naziv jezika je obavezan.")]
        [StringLength(200, ErrorMessage = "Naziv jezika može imati najviše 200 znakova.")]
        public required string Name { get; set; }
    }
}
