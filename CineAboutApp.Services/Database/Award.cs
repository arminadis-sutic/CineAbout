namespace CineAboutApp.Services.Database
{
    public class Award
    {
        public int Id { get; set; }

        public int AwardTypeId { get; set; }
        public AwardType AwardType { get; set; } = null!;

        public int? MovieId { get; set; }
        public Movie? Movie { get; set; }

        public int? PersonId { get; set; }
        public Person? Person { get; set; }

        public int Year { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
