using CineAboutApp.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.SearchObjects
{
    public class FreeQuizRequest
    {
        public string? Category { get; set; }
        public QuizDifficulty? Difficulty { get; set; }

        [Range(3, 20, ErrorMessage = "Broj pitanja mora biti između 3 i 20.")]
        public int Count { get; set; } = 5;
    }

    public enum LeaderboardPeriod
    {
        AllTime,
        Weekly,
        Daily
    }
}
