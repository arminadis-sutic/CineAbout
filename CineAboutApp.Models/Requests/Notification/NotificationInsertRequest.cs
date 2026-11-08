using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Notification
{
    public class NotificationInsertRequest
    {
        [Required(ErrorMessage = "Naslov je obavezan.")]
        [StringLength(200, ErrorMessage = "Naslov može imati najviše 200 znakova.")]
        public required string Title { get; set; }

        [StringLength(2000, ErrorMessage = "Tekst može imati najviše 2000 znakova.")]
        public string Body { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Adresa slike može imati najviše 500 znakova.")]
        public string? ImageUrl { get; set; }

        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();

        public virtual List<int> UserIds { get; set; } = [];
    }
}
