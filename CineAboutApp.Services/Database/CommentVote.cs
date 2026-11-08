namespace CineAboutApp.Services.Database
{
    public class CommentVote
    {
        public int Id { get; set; }

        public int CommentId { get; set; }
        public Comment Comment { get; set; } = null!;

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        public bool IsUpvote { get; set; }
    }
}
