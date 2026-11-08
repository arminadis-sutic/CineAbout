using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Responses
{
    public class PersonResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public PersonRoleType RoleType { get; set; }
        public DateTime BirthDate { get; set; }
        public string Biography { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
    }
}
