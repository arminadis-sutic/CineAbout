using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Thread
{
    public class CommentInsertRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Diskusija nije odabrana.")]
        public int ThreadId { get; set; }

        [Required(ErrorMessage = "Komentar ne može biti prazan.")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Komentar može imati između 1 i 2000 znakova.")]
        public string Content { get; set; } = string.Empty;
    }

    public class CommentUpdateRequest
    {
        [Required(ErrorMessage = "Komentar ne može biti prazan.")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Komentar može imati između 1 i 2000 znakova.")]
        public string Content { get; set; } = string.Empty;
    }

    public class VoteRequest
    {
        public bool IsUpvote { get; set; } = true;
    }
}
