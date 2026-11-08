using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class Country
    {
        [Key]
        public int Id { get; set; }
        public required string Name { get; set; }
    }
}
