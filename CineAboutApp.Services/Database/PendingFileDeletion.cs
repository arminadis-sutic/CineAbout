using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class PendingFileDeletion
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(400)]
        public string RelativePath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int Attempts { get; set; }

        [MaxLength(400)]
        public string? LastError { get; set; }
    }
}
