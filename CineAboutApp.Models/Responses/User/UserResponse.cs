namespace CineAboutApp.Models.Responses.User
{
    public class UserResponse
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public required string Username { get; set; }

        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }

        public string? ProfileImageUrl { get; set; }
        public CountryResponse? Country { get; set; }

        public List<RoleResponse> Roles { get; set; } = new();

        public UserProfileResponse? Profile { get; set; }
    }
}
