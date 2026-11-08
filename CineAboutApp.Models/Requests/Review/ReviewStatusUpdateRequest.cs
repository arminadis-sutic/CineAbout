using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Requests.Review
{
    public class ReviewStatusUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.EnumDataType(typeof(ReviewStatus), ErrorMessage = "Status nije ispravan.")]
        public ReviewStatus Status { get; set; }
    }
}
