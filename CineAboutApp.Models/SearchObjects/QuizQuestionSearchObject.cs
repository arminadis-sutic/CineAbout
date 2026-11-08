using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.SearchObjects
{
    public class QuizQuestionSearchObject : BaseSearchObject
    {
        public QuizDifficulty? Difficulty { get; set; }
        public string? Category { get; set; }
        public int? CategoryId { get; set; }
    }
}
