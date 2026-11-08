using CineAboutApp.Models.Enums;

namespace CineAboutApp.Services.Database
{
    public class Review
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        public int Rating { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ReviewStatus Status { get; set; }

        public ICollection<ReviewVote> ReviewVotes { get; set; } = new List<ReviewVote>();
    }
}
