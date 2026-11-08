namespace CineAboutApp.Models.SearchObjects
{
    public class NotificationSearchObject : BaseSearchObject
    {
        public bool? IsRead { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
    }
}
