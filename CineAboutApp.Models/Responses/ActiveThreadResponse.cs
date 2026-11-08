namespace CineAboutApp.Models.Responses
{
    public class ActiveThreadResponse
    {
        public int ThreadId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorUsername { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public int CommentCount { get; set; }
        public int VoteCount { get; set; }
        public bool HasSpoiler { get; set; }
    }
}