namespace CineAboutApp.Models.Requests
{
    public class PasswordResetRequest
    {
        public string Password { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
    }
}
