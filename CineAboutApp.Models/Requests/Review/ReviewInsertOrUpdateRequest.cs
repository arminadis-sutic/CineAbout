using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Review
{
    public class ReviewInsertOrUpdateRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Odaberite film za koji pišete recenziju.")]
        public int MovieId { get; set; }

        [Range(0, 10, ErrorMessage = "Ocjena mora biti cijeli broj od 0 do 10.")]
        public int Rating { get; set; }

        [StringLength(4000, ErrorMessage = "Tekst recenzije može imati najviše 4000 znakova.")]
        public string Text { get; set; } = string.Empty;
    }
}
