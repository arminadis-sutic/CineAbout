namespace CineAboutApp.Models.SearchObjects
{
    public class UserSearchObject : BaseSearchObject
    {
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
    }
}
