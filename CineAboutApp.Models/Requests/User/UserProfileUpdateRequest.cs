namespace CineAboutApp.Models.Requests.User
{
    public class UserProfileUpdateRequest
    {
        public string? Bio { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }
        public bool? RemoveImage { get; set; }
        public int? CityId { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? IsProfilePublic { get; set; }
        public bool? IsPhonePublic { get; set; }
        public bool? IsEmailPublic { get; set; }
        public bool? EmailNotificationsEnabled { get; set; }
        public bool? PushNotificationsEnabled { get; set; }
    }
}
