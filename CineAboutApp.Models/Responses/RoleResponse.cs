using CineAboutApp.Models.Responses.User;
using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Responses
{
    public class RoleResponse
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
        public List<UserResponse> Users { get; set; } = new();
    }
}
