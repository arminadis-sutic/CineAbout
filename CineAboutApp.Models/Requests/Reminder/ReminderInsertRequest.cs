using System.ComponentModel.DataAnnotations;

namespace CineAboutApp.Models.Requests.Reminder
{
    public class ReminderInsertRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Naslov nije odabran.")]
        public int TmdbId { get; set; }
    }
}
