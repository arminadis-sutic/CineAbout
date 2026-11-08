namespace CineAboutApp.Models.Requests.User
{
    public class RefreshRequest
    {
        public string JwtToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
