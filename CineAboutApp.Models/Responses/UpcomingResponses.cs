namespace CineAboutApp.Models.Responses
{
    public class UpcomingTitleResponse
    {
        public int TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Overview { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public int DaysUntilRelease { get; set; }
        public bool HasReminder { get; set; }
    }

    public class ReminderResponse
    {
        public int Id { get; set; }
        public int TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public int DaysUntilRelease { get; set; }
        public bool NotificationSent { get; set; }
    }
}
