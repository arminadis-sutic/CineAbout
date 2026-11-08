namespace CineAboutApp.Services.Database
{
    public class Follow
    {
        public int Id { get; set; }

        public int FollowerId { get; set; }
        public MyAppUser Follower { get; set; } = null!;

        public int FollowingId { get; set; }
        public MyAppUser Following { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
