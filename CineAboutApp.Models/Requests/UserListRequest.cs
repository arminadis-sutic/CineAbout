using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests
{
    public class UserListRequest
    {
        [Required(ErrorMessage = "Naziv liste je obavezan.")]
        [StringLength(100, ErrorMessage = "Naziv liste može imati najviše 100 znakova.")]
        public required string Name { get; set; }
    }
}
