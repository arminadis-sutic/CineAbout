namespace CineAboutApp.Interfaces.General
{
    public interface IIdentityService
    {
        int GetCurrentUserId();
        int? GetCurrentUserIdNullable();
        bool IsAdmin();
        bool IsAuthenticated();
        IEnumerable<string> GetCurrentUserRoles();
        bool IsInRole(string roleName);
    }
}
