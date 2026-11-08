namespace CineAboutApp.Models.Requests.User
{
    public class RegisterDeviceTokenRequest
    {
        public string DeviceToken { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
    }
}
