namespace CineAboutApp.Models.Responses
{
    public class CityResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int CountryId { get; set; }
        public string CountryName { get; set; } = string.Empty;
    }
}
