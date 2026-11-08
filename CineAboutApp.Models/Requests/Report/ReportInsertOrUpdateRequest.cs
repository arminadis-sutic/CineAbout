using CineAboutApp.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Report
{
    public class ReportInsertOrUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.EnumDataType(typeof(ReportTargetType), ErrorMessage = "Vrsta sadržaja nije ispravna.")]
        public ReportTargetType TargetType { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sadržaj koji se prijavljuje nije odabran.")]
        public int TargetId { get; set; }

        [Required(ErrorMessage = "Razlog prijave je obavezan.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Razlog prijave mora imati između 3 i 500 znakova.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReportDecisionRequest
    {
        [System.ComponentModel.DataAnnotations.EnumDataType(typeof(ReportStatus), ErrorMessage = "Status nije ispravan.")]
        public ReportStatus Status { get; set; }

        public bool RemoveContent { get; set; }

        [StringLength(500, ErrorMessage = "Napomena može imati najviše 500 znakova.")]
        public string? AdminNote { get; set; }
    }
}
