using System.ComponentModel.DataAnnotations;
using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Requests.Movie
{
    public class MovieInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Naslov je obavezan.")]
        [StringLength(200, ErrorMessage = "Naslov može imati najviše 200 znakova.")]
        public required string Title { get; set; }

        [StringLength(200, ErrorMessage = "Originalni naslov može imati najviše 200 znakova.")]
        public string OriginalTitle { get; set; } = string.Empty;

        [EnumDataType(typeof(MovieType), ErrorMessage = "Tip naslova nije ispravan.")]
        public MovieType Type { get; set; }

        [Range(1888, 2100, ErrorMessage = "Godina izlaska mora biti između 1888. i 2100.")]
        public int ReleaseYear { get; set; }

        [Range(1, 1000, ErrorMessage = "Trajanje mora biti između 1 i 1000 minuta.")]
        public int Duration { get; set; }

        [StringLength(4000, ErrorMessage = "Sinopsis može imati najviše 4000 znakova.")]
        public string Synopsis { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Adresa postera može imati najviše 500 znakova.")]
        public string PosterUrl { get; set; } = string.Empty;

        public bool IsVisible { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Odaberite jezik.")]
        public int LanguageId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Odaberite studio.")]
        public int StudioId { get; set; }

        [MaxLength(30, ErrorMessage = "Možete odabrati najviše 30 žanrova.")]
        public List<int> GenreIds { get; set; } = new List<int>();

        [MaxLength(100, ErrorMessage = "Možete dodati najviše 100 članova ekipe.")]
        public List<MovieCastRequest> Cast { get; set; } = new List<MovieCastRequest>();
    }

    public class MovieCastRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Odaberite osobu.")]
        public int PersonId { get; set; }

        [StringLength(200, ErrorMessage = "Uloga može imati najviše 200 znakova.")]
        public string RoleInMovie { get; set; } = string.Empty;
    }
}
