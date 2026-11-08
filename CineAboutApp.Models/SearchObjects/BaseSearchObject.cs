namespace CineAboutApp.Models.SearchObjects
{
    public class BaseSearchObject
    {
        public string? NameFTS { get; set; }
        public bool? IsDeleted { get; set; } = false;
        public int? PageNumber { get; set; } = 1;

        private int? _pageSize = 20;
        public int? PageSize
        {
            get => _pageSize;
            set => _pageSize = value.HasValue ? (value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value)) : 20;
        }

        public const int MaxPageSize = 100;
    }
}
