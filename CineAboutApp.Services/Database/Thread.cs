using CineAboutApp.Models.Enums;

namespace CineAboutApp.Services.Database
{
    public class Thread
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool HasSpoiler { get; set; }
        public ThreadStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? MovieId { get; set; }
        public Movie? Movie { get; set; }
        public ICollection<ThreadTag> ThreadTags { get; set; } = new List<ThreadTag>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<ThreadVote> ThreadVotes { get; set; } = new List<ThreadVote>();
    }
}
