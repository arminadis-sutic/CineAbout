using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Role
{
    public class RoleInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naziv uloge je obavezan.")]
        [StringLength(50, ErrorMessage = "Naziv uloge može imati najviše 50 znakova.")]
        public required string Name { get; set; }
    }
}
