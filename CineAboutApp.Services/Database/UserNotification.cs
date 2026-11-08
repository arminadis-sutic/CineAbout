using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineAboutApp.Services.Database
{
    public class UserNotification
    {
        [Key]
        public int Id { get; set; }
        public int NotificationId {  get; set; }

        [ForeignKey(nameof(NotificationId))]
        public Notification? Notification { get; set; }
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public MyAppUser? User { get; set; }
        public DateTime? SeenAt { get; set; }

    }
}
