using CineAboutApp.Models.Enums;

namespace CineAboutApp.Services.Database
{
    public class SwipeHistory
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        public SwipeDirection Direction { get; set; }
        public DateTime SwipedAt { get; set; }
    }
}
