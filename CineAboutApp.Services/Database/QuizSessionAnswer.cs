namespace CineAboutApp.Services.Database
{
    public class QuizSessionAnswer
    {
        public int Id { get; set; }

        public int QuizSessionId { get; set; }
        public QuizSession QuizSession { get; set; } = null!;

        public int QuizQuestionId { get; set; }
        public QuizQuestion QuizQuestion { get; set; } = null!;

        public string SelectedOption { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public double ResponseSeconds { get; set; }
    }
}
