using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class AppSettings
    {
        [Key]
        public int Id { get; set; }
        public bool AutoActivateNewUsers { get; set; } = false;
    }
}
