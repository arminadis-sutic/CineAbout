using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class Role
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public virtual ICollection<UserRole>? Users { get; set; }
    }
}
