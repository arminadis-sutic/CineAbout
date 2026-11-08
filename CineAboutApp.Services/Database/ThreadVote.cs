namespace CineAboutApp.Services.Database
{
    public class ThreadVote
    {
        public int Id { get; set; }

        public int ThreadId { get; set; }
        public Thread Thread { get; set; } = null!;

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        public bool IsUpvote { get; set; }
    }
}
