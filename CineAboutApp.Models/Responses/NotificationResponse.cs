namespace CineAboutApp.Models.Responses
{
    public class NotificationResponse
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public DateTime CreatedAt { get; set; }
        public DateTime? SeenAt { get; set; }
    }
}
