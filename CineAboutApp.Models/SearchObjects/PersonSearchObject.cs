using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.SearchObjects
{
    public class PersonSearchObject : BaseSearchObject
    {
        public PersonRoleType? RoleType { get; set; }
    }
}
