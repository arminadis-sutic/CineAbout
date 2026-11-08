namespace CineAboutApp.Interfaces.General
{
    public interface IUserSessionService
    {
        Task<bool> IsSessionValidAsync(int userId, int securityStamp, CancellationToken cancellationToken = default);
    }
}
