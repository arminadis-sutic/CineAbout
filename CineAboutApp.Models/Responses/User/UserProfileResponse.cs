namespace CineAboutApp.Models.Responses.User
{
    public class UserProfileResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? City { get; set; }
        public int? CityId { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool IsProfilePublic { get; set; }
        public bool IsPhonePublic { get; set; }
        public bool IsEmailPublic { get; set; }
        public bool EmailNotificationsEnabled { get; set; }
        public bool PushNotificationsEnabled { get; set; }
        public DateTime? LastActivity { get; set; }

        public int RatedMoviesCount { get; set; }
        public int ReviewsCount { get; set; }
        public int ThreadsCount { get; set; }
        public int CommentsCount { get; set; }
        public int CurrentStreak { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
    }
}
