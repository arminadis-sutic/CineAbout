namespace CineAboutApp.Models.Responses
{
    public class DashboardActivityResponse
    {
        public DateTime Date { get; set; }
        public int NewUsers { get; set; }
        public int Reviews { get; set; }
        public int Threads { get; set; }
        public int Comments { get; set; }
        public int QuizSessions { get; set; }

        public int TotalActivity =>
            NewUsers + Reviews + Threads + Comments + QuizSessions;
    }
}