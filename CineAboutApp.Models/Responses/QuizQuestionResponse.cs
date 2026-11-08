using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Responses
{
    public class QuizQuestionResponse
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectOption { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string Category { get; set; } = string.Empty;
        public QuizDifficulty Difficulty { get; set; }
    }
}
