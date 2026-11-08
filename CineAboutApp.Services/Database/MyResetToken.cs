using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineAboutApp.Services.Database
{
    public class MyResetToken
    {
        [Key]
        public int Id { get; set; }
        public required string ResetToken { get; set; }
        public DateTime ExpiryTime { get; set; }
        public int MyAppUserId { get; set; }

        [ForeignKey(nameof(MyAppUserId))]
        public MyAppUser? MyAppUser { get; set; }
    }
}
