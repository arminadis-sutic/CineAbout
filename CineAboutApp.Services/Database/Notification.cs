using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public required string Title { get; set; }
        public string Body { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public DateTime CreatedAt { get; set; }
        public virtual List<UserNotification> Users { get; set; } = [];
    }
}
