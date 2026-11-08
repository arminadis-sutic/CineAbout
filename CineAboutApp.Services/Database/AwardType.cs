namespace CineAboutApp.Services.Database
{
    public class AwardType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Award> Awards { get; set; } = new List<Award>();
    }
}
