using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineAboutApp.Services.Database
{
    public class MyAppUser : ISoftDeletable
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public required string FirstName { get; set; }
        [Required]
        public required string LastName { get; set; }
        [Required]
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string PasswordSalt { get; set; }
        [Required]
        public required string Username { get; set; }
        public bool IsActive { get; set; }

        public int SecurityStamp { get; set; }
        public DateTime RegistrationDate { get; set; }
        
        public int CountryId { get; set; }
        public DateTime? LastActivity { get; set; }

        [ForeignKey(nameof(CountryId))]
        public Country? Country { get; set; } = null;
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<UserDeviceToken> DeviceTokens { get; set; } = new List<UserDeviceToken>();
        public virtual UserProfile? UserProfile { get; set; }
        public virtual ICollection<Follow> Followers { get; set; } = new List<Follow>();
        public virtual ICollection<Follow> Following { get; set; } = new List<Follow>();
        public virtual Streak? Streak { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }
    }
}

