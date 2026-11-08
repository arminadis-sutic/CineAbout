using CineAboutApp.Models.Enums;

namespace CineAboutApp.Services.Database
{
    public class Person
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public PersonRoleType RoleType { get; set; }
        public DateTime BirthDate { get; set; }
        public string Biography { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;

        public ICollection<MovieCast> MovieCasts { get; set; } = new List<MovieCast>();
        public ICollection<Award> Awards { get; set; } = new List<Award>();
    }
}
