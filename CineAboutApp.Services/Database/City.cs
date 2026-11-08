using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class City
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int CountryId { get; set; }
        public Country? Country { get; set; }
    }
}
