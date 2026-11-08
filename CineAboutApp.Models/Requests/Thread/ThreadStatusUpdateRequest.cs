using CineAboutApp.Models.Enums;

namespace CineAboutApp.Models.Requests.Thread
{
    public class ThreadStatusUpdateRequest
    {
        [System.ComponentModel.DataAnnotations.EnumDataType(typeof(ThreadStatus), ErrorMessage = "Status nije ispravan.")]
        public ThreadStatus Status { get; set; }
    }
}
