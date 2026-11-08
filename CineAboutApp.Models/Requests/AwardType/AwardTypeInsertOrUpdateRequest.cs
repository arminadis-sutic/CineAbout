using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.AwardType
{
    public class AwardTypeInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naziv tipa nagrade je obavezan.")]
        [StringLength(200, ErrorMessage = "Naziv tipa nagrade može imati najviše 200 znakova.")]
        public required string Name { get; set; }
    }
}
