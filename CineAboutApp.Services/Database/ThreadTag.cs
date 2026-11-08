namespace CineAboutApp.Services.Database
{
    public class ThreadTag
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public Thread Thread { get; set; } = null!;
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
