namespace CineAboutApp.Models.Responses
{
    public class DashboardKpiResponse
    {
        public int TotalUsers { get; set; }
        public int NewUsersLast7Days { get; set; }
        public int TotalReviews { get; set; }
        public int ActiveThreads { get; set; }
        public int TotalMovies { get; set; }
        public int TotalQuizSessions { get; set; }

        public int UsersTrendPercent { get; set; }
        public int ReviewsTrendPercent { get; set; }
        public int ThreadsTrendPercent { get; set; }
        public int QuizSessionsTrendPercent { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}