using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Thread
{
    public class ThreadInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naslov je obavezan.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Naslov mora imati između 3 i 200 znakova.")]
        public required string Title { get; set; }

        [StringLength(8000, ErrorMessage = "Sadržaj može imati najviše 8000 znakova.")]
        public string Content { get; set; } = string.Empty;

        public bool HasSpoiler { get; set; }

        public int? MovieId { get; set; }

        [MaxLength(5, ErrorMessage = "Možete dodati najviše 5 tagova.")]
        public List<string> Tags { get; set; } = new List<string>();
    }
}
