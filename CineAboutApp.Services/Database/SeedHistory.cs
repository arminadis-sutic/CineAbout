using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class SeedHistory
    {
        [Key, MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        public DateTime AppliedAt { get; set; }

        [MaxLength(200)]
        public string? Note { get; set; }
    }
}
