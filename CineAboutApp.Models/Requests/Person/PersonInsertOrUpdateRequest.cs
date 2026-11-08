using System.ComponentModel.DataAnnotations;
using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Requests.Person
{
    public class PersonInsertOrUpdateRequest
    {
        [Required(ErrorMessage = "Ime i prezime je obavezno.")]
        [StringLength(200, ErrorMessage = "Ime može imati najviše 200 znakova.")]
        public required string FullName { get; set; }

        [EnumDataType(typeof(PersonRoleType), ErrorMessage = "Uloga osobe nije ispravna.")]
        public PersonRoleType RoleType { get; set; }

        [NotInFutureDate(1850, ErrorMessage = "Datum rođenja mora biti između 1850. godine i danas.")]
        public DateTime BirthDate { get; set; }

        [StringLength(4000, ErrorMessage = "Biografija može imati najviše 4000 znakova.")]
        public string Biography { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Adresa fotografije može imati najviše 500 znakova.")]
        public string PhotoUrl { get; set; } = string.Empty;
    }
}
