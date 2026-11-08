using CineAboutApp.Models.Enums;

namespace CineAboutApp.Services.Database
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectOption { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public QuizCategory? Category { get; set; }
        public QuizDifficulty Difficulty { get; set; }

        public ICollection<QuizSessionAnswer> QuizSessionAnswers { get; set; } = new List<QuizSessionAnswer>();
    }
}
