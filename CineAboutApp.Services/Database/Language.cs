namespace CineAboutApp.Services.Database
{
    public class Language
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Movie> Movies { get; set; } = new List<Movie>();
    }
}
