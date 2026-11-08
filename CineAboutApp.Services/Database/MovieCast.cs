namespace CineAboutApp.Services.Database
{
    public class MovieCast
    {
        public int Id { get; set; }

        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        public int PersonId { get; set; }
        public Person Person { get; set; } = null!;

        public string RoleInMovie { get; set; } = string.Empty;
    }
}
