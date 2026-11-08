using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineAboutApp.Services.Database
{
    public class UserProfile
    {
        [Key]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public MyAppUser? User { get; set; }

        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public int? CityId { get; set; }
        public City? City { get; set; }
        public string? Phone { get; set; }
        
        
        public DateTime? DateOfBirth { get; set; }
        
        public bool IsProfilePublic { get; set; } = true;
        
        
        public bool IsPhonePublic { get; set; } = false;
        public bool IsEmailPublic { get; set; } = false;
        
        
        

        public bool EmailNotificationsEnabled { get; set; } = true;
        public bool PushNotificationsEnabled { get; set; } = true;

    }

   


}
