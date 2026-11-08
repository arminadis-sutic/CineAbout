namespace CineAboutApp.Services.Database
{
    public class Reminder
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;
        public int TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public bool NotificationSent { get; set; }
    }
}
