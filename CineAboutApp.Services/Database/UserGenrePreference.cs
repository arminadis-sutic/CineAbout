namespace CineAboutApp.Services.Database
{
    public class UserGenrePreference
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;
        public int GenreId { get; set; }
        public Genre Genre { get; set; } = null!;
    }
}
