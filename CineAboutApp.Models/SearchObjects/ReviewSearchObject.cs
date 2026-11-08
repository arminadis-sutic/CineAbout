using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.SearchObjects
{
    public enum ReviewSortBy
    {
        Relevance,
        Date,
        Rating
    }

    public class ReviewSearchObject : BaseSearchObject
    {
        public ReviewStatus? Status { get; set; }
        public int? Rating { get; set; }
        public int? MovieId { get; set; }
        public int? UserId { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public ReviewSortBy SortBy { get; set; } = ReviewSortBy.Relevance;
    }
}
