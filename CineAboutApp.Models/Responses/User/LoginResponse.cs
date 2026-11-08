using System.Text.Json.Serialization;

namespace CineAboutApp.Models.Responses.User
{
    public class LoginResponse
    {
        [JsonPropertyName("jwtToken")]
        public string JwtToken { get; set; } = string.Empty;
        
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
