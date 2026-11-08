using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Services.Database
{
    public class QuizCategory
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }
}
