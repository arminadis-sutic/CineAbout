namespace CineAboutApp.Models.Messages
{
    public class PushNotification
    {
        public required List<string> Tokens { get; set; }
        public required string Title { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}
