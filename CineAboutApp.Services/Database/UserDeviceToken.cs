using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineAboutApp.Services.Database
{
    public class UserDeviceToken
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public MyAppUser? User { get; set; }
        public required string Token { get; set; }
        public required string DeviceType { get; set; }
        public DateTime LastLoggedOn { get; set; }
    }
}
