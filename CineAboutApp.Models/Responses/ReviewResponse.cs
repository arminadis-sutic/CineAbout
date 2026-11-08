using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Responses
{
    public class ReviewResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? UserImageUrl { get; set; }
        public int MovieId { get; set; }
        public string MovieTitle { get; set; } = string.Empty;
        public string? MoviePosterUrl { get; set; }
        public int Rating { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ReviewStatus Status { get; set; }
        public int HelpfulCount { get; set; }
        public int NotHelpfulCount { get; set; }
        public bool? MyVote { get; set; }
    }

    public class RatingSummaryResponse
    {
        public int MovieId { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int[] Histogram { get; set; } = new int[11];
        public int? MyRating { get; set; }
        public int? MyReviewId { get; set; }
    }
}
