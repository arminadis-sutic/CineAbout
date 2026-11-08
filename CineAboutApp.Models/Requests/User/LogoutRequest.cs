namespace CineAboutApp.Models.Requests.User
{
    public class LogoutRequest
    {
        public string JwtToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string? DeviceToken { get; set; }
    }
}
