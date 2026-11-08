using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Responses
{
    public class QuizPlayQuestionResponse
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public QuizDifficulty Difficulty { get; set; }
    }

    public class QuizStartResponse
    {
        public int SessionId { get; set; }
        public QuizMode Mode { get; set; }
        public int SecondsPerQuestion { get; set; }
        public List<QuizPlayQuestionResponse> Questions { get; set; } = new List<QuizPlayQuestionResponse>();
    }

    public class QuizAnswerResultResponse
    {
        public int QuestionId { get; set; }
        public bool IsCorrect { get; set; }
        public string CorrectOption { get; set; } = string.Empty;
        public int Points { get; set; }
    }

    public class QuizResultResponse
    {
        public int SessionId { get; set; }
        public int Score { get; set; }
        public int CorrectCount { get; set; }
        public int QuestionCount { get; set; }
        public int CurrentStreak { get; set; }
        public List<QuizAnswerResultResponse> Answers { get; set; } = new List<QuizAnswerResultResponse>();
    }

    public class StreakResponse
    {
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public bool PlayedToday { get; set; }
    }

    public class LeaderboardEntryResponse
    {
        public int Rank { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public int TotalScore { get; set; }
        public int SessionsPlayed { get; set; }
        public int CurrentStreak { get; set; }
    }

    public class LeaderboardResponse
    {
        public List<LeaderboardEntryResponse> Entries { get; set; } = new List<LeaderboardEntryResponse>();
        public LeaderboardEntryResponse? Me { get; set; }
    }
}
