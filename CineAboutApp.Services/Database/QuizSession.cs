using CineAboutApp.Models.Enums;

namespace CineAboutApp.Services.Database
{
    public class QuizSession
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        public DateTime PlayedAt { get; set; }
        public int Score { get; set; }
        public QuizMode Mode { get; set; }
        public bool IsCompleted { get; set; }

        public ICollection<QuizSessionAnswer> QuizSessionAnswers { get; set; } = new List<QuizSessionAnswer>();
    }
}
