namespace CineAboutApp.Models.Requests.User
{
    public class UserInsertOrUpdateRequest
    {
        public required string Username { get; set; }
        public string? Password { get; set; }
        public string? PasswordConfirmation { get; set; }
        public string? CurrentPassword { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int CountryId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int> RoleIds { get; set; } = new List<int>();
    }
}
