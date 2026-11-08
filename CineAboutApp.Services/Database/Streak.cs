namespace CineAboutApp.Services.Database
{
    public class Streak
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public DateTime LastPlayedDate { get; set; }
    }
}
