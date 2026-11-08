using System.ComponentModel.DataAnnotations;
using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Requests.QuizQuestion
{
    public class QuizQuestionInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Tekst pitanja je obavezan.")]
        [StringLength(500, ErrorMessage = "Pitanje može imati najviše 500 znakova.")]
        public required string Text { get; set; }

        [Required(ErrorMessage = "Odgovor A je obavezan.")]
        [StringLength(200, ErrorMessage = "Odgovor može imati najviše 200 znakova.")]
        public string OptionA { get; set; } = string.Empty;

        [Required(ErrorMessage = "Odgovor B je obavezan.")]
        [StringLength(200, ErrorMessage = "Odgovor može imati najviše 200 znakova.")]
        public string OptionB { get; set; } = string.Empty;

        [Required(ErrorMessage = "Odgovor C je obavezan.")]
        [StringLength(200, ErrorMessage = "Odgovor može imati najviše 200 znakova.")]
        public string OptionC { get; set; } = string.Empty;

        [Required(ErrorMessage = "Odgovor D je obavezan.")]
        [StringLength(200, ErrorMessage = "Odgovor može imati najviše 200 znakova.")]
        public string OptionD { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tačan odgovor je obavezan.")]
        [RegularExpression("^[A-Da-d]$", ErrorMessage = "Tačan odgovor mora biti A, B, C ili D.")]
        public string CorrectOption { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Odaberite kategoriju.")]
        public int CategoryId { get; set; }

        [EnumDataType(typeof(QuizDifficulty), ErrorMessage = "Težina nije ispravna.")]
        public QuizDifficulty Difficulty { get; set; }
    }
}
