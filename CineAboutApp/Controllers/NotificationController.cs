using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests.Notification;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    public class NotificationController(INotificationService service, IIdentityService identityService) 
        : BaseCRUDController<NotificationResponse, NotificationSearchObject, NotificationInsertRequest, NotificationInsertRequest>(service)
    {
        [HttpGet]
        [Authorize]
        public override Task<IActionResult> Get([FromQuery] NotificationSearchObject search, CancellationToken cancellationToken = default)
        {
            return base.Get(search, cancellationToken);
        }

        [HttpGet("{id}")]
        [Authorize]
        public override Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
        {
            return base.GetById(id, cancellationToken);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Create([FromBody] NotificationInsertRequest request, CancellationToken cancellationToken = default)
        {
            return base.Create(request, cancellationToken);
        }
        
        [HttpGet("unread-count")]
        [Authorize]
        public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
        {
            int userId = identityService.GetCurrentUserId();
            var count = await service.GetUnreadCount(userId, cancellationToken);
            return Ok(count);
        }


        [HttpPut("mark-as-read/{notificationId}")]
        [Authorize]
        public async Task<IActionResult> MarkAsRead([FromRoute] int notificationId, CancellationToken cancellationToken = default)
        {
            await service.MarkAsSeen(notificationId, cancellationToken);
            return Ok();
        }

        [HttpPut("mark-all-as-read")]
        [Authorize]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
        {
            await service.MarkAllAsSeen(cancellationToken);
            return Ok();
        }

        [NonAction]
        public override Task<IActionResult> Update([FromRoute] int id, [FromBody] NotificationInsertRequest request, CancellationToken cancellationToken = default)
        {
            return base.Update(id, request, cancellationToken);
        }

        [NonAction]
        public override Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
        {
            return base.Delete(id, cancellationToken);
        }

        [HttpDelete("{notificationId}")]
        [Authorize]
        public async Task<IActionResult> DeleteNotification([FromRoute] int notificationId, CancellationToken cancellationToken = default)
        {
            await service.DeleteUserNotification(notificationId, cancellationToken);
            return Ok();
        }
        [HttpDelete("delete-all")]
        [Authorize]
        public async Task<IActionResult> DeleteAll(CancellationToken cancellationToken = default)
        {
            await service.DeleteAllUserNotifications(cancellationToken);
            return Ok();
        }
    }
}
