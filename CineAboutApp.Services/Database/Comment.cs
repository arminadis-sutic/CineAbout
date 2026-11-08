namespace CineAboutApp.Services.Database
{
    public class Comment
    {
        public int Id { get; set; }

        public int ThreadId { get; set; }
        public Thread Thread { get; set; } = null!;

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public ICollection<CommentVote> CommentVotes { get; set; } = new List<CommentVote>();
    }
}
