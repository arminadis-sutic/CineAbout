namespace CineAboutApp.Interfaces.General
{
    public interface ITokenBlacklistService
    {
        Task BlacklistTokenAsync(string jti, DateTime expiryTime, int? userId = null);
        Task<bool> IsTokenBlacklistedAsync(string jti);
    }
}
