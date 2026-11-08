using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Quiz
{
    public class QuizSubmitRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Kviz sesija nije ispravna.")]
        public int SessionId { get; set; }

        [Required(ErrorMessage = "Odgovori su obavezni.")]
        public List<QuizAnswerRequest> Answers { get; set; } = new List<QuizAnswerRequest>();
    }

    public class QuizAnswerRequest
    {
        public int QuestionId { get; set; }

        [RegularExpression("^[A-Da-d]?$", ErrorMessage = "Odgovor mora biti A, B, C ili D.")]
        public string SelectedOption { get; set; } = string.Empty;

        [Range(0, 120, ErrorMessage = "Vrijeme odgovora mora biti između 0 i 120 sekundi.")]
        public double ResponseSeconds { get; set; }
    }
}
