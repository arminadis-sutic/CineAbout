using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class Tag
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public ICollection<ThreadTag> ThreadTags { get; set; } = new List<ThreadTag>();
    }
}
