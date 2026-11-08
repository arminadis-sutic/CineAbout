using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Entities
{
    public class BlacklistedToken
    {
        [Key]
        public int Id { get; set; }
        public required string Jti { get; set; }
        public DateTime ExpiryTime { get; set; }
        public DateTime BlacklistedAt { get; set; } = DateTime.UtcNow;
        public int? UserId { get; set; }
    }
}
