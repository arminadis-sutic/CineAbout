using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.SearchObjects
{
    public enum ThreadSortBy
    {
        Newest,
        Top
    }

    public class ThreadSearchObject : BaseSearchObject
    {
        public ThreadStatus? Status { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int? MovieId { get; set; }
        public int? UserId { get; set; }
        public string? Tags { get; set; }
        public ThreadSortBy SortBy { get; set; } = ThreadSortBy.Newest;
    }
}
