namespace CineAboutApp.Models.Responses
{
    public class FollowUserResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public bool IsFollowedByCurrentUser { get; set; }
    }

    public class FeedItemResponse
    {
        public string Type { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? UserImageUrl { get; set; }
        public int? MovieId { get; set; }
        public string? MovieTitle { get; set; }
        public string? MoviePosterUrl { get; set; }
        public int? Rating { get; set; }
        public string? Text { get; set; }
        public int? ThreadId { get; set; }
        public int? ListId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
