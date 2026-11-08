using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.QuizCategory
{
    public class QuizCategoryInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naziv kategorije je obavezan.")]
        [StringLength(200, ErrorMessage = "Naziv kategorije može imati najviše 200 znakova.")]
        public required string Name { get; set; }
    }
}
