using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class UserList
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserListItem> Items { get; set; } = new List<UserListItem>();
    }

    public class UserListItem
    {
        [Key]
        public int Id { get; set; }

        public int UserListId { get; set; }
        public UserList UserList { get; set; } = null!;

        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
