using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Responses
{
    public class ThreadResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool HasSpoiler { get; set; }
        public ThreadStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? MovieId { get; set; }
        public string? MovieTitle { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public int Score { get; set; }
        public int CommentCount { get; set; }
        public int MyVote { get; set; }
        public List<CommentResponse> Comments { get; set; } = new List<CommentResponse>();
    }

    public class CommentResponse
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Score { get; set; }
        public int MyVote { get; set; }
    }
}
