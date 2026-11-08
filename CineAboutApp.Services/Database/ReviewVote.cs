namespace CineAboutApp.Services.Database
{
    public class ReviewVote
    {
        public int Id { get; set; }

        public int ReviewId { get; set; }
        public Review Review { get; set; } = null!;

        public int UserId { get; set; }
        public MyAppUser User { get; set; } = null!;

        public bool IsHelpful { get; set; }
    }
}
