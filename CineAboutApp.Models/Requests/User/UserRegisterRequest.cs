namespace CineAboutApp.Models.Requests.User
{
    public class UserRegisterRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? PasswordConfirmation { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public int CountryId { get; set; }
    }
}
