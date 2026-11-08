namespace CineAboutApp.Models.Responses.User
{
    public class UserSearchResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public bool IsFollowedByCurrentUser { get; set; }
        public string? City { get; set; }
        public int? CityId { get; set; }
    }
}
