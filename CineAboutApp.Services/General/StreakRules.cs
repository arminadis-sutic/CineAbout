using CineAboutApp.Services.Database;

namespace CineAboutApp.Services.General
{
    public static class StreakRules
    {
        public static int Effective(Streak? streak) =>
            streak != null && streak.LastPlayedDate.Date >= DateTime.UtcNow.Date.AddDays(-1) ? streak.CurrentStreak : 0;
    }
}
