using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using Microsoft.Extensions.Logging;
using CineAboutApp.Model;
using CineAboutApp.Models.Messages;
using CineAboutApp.Models.Events;
using CineAboutApp.Models.Requests.Notification;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using CineAboutApp.Services.Database;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;

namespace CineAboutApp.Services.Controller
{
    public class NotificationService(ApplicationDbContext db, IMapper mapper, IRabbitMQHandler rmqh, IConfiguration cfg, ICacheService cacheService, IIdentityService identityService, ILogger<NotificationService> logger, IHubContext<CineAboutApp.Services.Hubs.NotificationHub>? hubContext = null) 
        : BaseCRUDService<NotificationResponse, NotificationSearchObject, NotificationInsertRequest, NotificationInsertRequest, Notification>(db, mapper), INotificationService
    {

        protected override async Task BeforeCreate(Notification entity, NotificationInsertRequest request, CancellationToken cancellationToken = default)
        {
            entity.CreatedAt = DateTime.UtcNow;
            await base.BeforeCreate(entity, request, cancellationToken);
        }

        public override async Task<NotificationResponse> CreateAsync(NotificationInsertRequest request, CancellationToken cancellationToken = default)
        {
            var existingTransaction = db.Database.CurrentTransaction;
            using var transaction = existingTransaction == null 
                ? await db.Database.BeginTransactionAsync(cancellationToken) 
                : null;
                
            try
            {
                var res = await base.CreateAsync(request, cancellationToken);

                if (request.UserIds.Count > 0)
                {
                    await db.UserNotifications.AddRangeAsync(request.UserIds.Select(u => new UserNotification()
                    {
                        NotificationId = res.Id,
                        SeenAt = null,
                        UserId = u
                    }));
                    await db.SaveChangesAsync(cancellationToken);

                    res.Data.Add("notificationId", res.Id.ToString());

                    var tokens = await db.UserNotifications.Include(u => u.User).ThenInclude(u => u!.DeviceTokens)
                        .Where(u => u.NotificationId == res.Id && (u.User!.UserProfile == null || u.User.UserProfile.PushNotificationsEnabled))
                        .SelectMany(u => u.User!.DeviceTokens.Select(dt => dt.Token)).Distinct().ToListAsync(cancellationToken);
                    
                    string? imageUrl = res.ImageUrl;
                    if (!string.IsNullOrWhiteSpace(imageUrl) && !imageUrl.StartsWith("http"))
                    {
                        string baseUrl = cfg.GetValue<string>("BackendUrl") ?? "https://localhost:7008";
                        imageUrl = $"{baseUrl}/{imageUrl.TrimStart('/')}";
                    }

                    await rmqh.PublishAsync(new PushNotification()
                    {
                        Body = res.Body,
                        Title = res.Title,
                        ImageUrl = imageUrl,
                        Data = res.Data,
                        Tokens = tokens,
                    });

                    if (transaction != null) await transaction.CommitAsync(cancellationToken);

                    foreach (var userId in request.UserIds)
                    {
                        try
                        {
                            await cacheService.RemoveAsync($"notifications_user_{userId}");
                            
                            if (hubContext != null)
                            {
                                await hubContext.Clients.User(userId.ToString()).SendAsync("NewNotification", res);
                                var unreadCount = await db.UserNotifications.CountAsync(un => un.UserId == userId && un.SeenAt == null, cancellationToken);
                                await hubContext.Clients.User(userId.ToString()).SendAsync("UnreadCountUpdated", new { unreadCount });
                            }

                            await rmqh.PublishAsync(new SignalRNotificationEvent
                            {
                                UserIds = new List<int> { userId },
                                MethodName = "NewNotification",
                                Payload = res
                            });

                            var finalUnreadCount = await db.UserNotifications.CountAsync(un => un.UserId == userId && un.SeenAt == null, cancellationToken);
                            await rmqh.PublishAsync(new SignalRNotificationEvent
                            {
                                UserIds = new List<int> { userId },
                                MethodName = "UnreadCountUpdated",
                                Payload = new { unreadCount = finalUnreadCount }
                            });
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to remove notification cache or broadcast SignalR for user {UserId}", userId);
                        }
                    }
                }
                else
                {
                    if (transaction != null) await transaction.CommitAsync(cancellationToken);
                }

                return res;
            }
            catch (Exception)
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public override async Task<MyPagedList<NotificationResponse>> GetAsync(NotificationSearchObject search, CancellationToken cancellation = default)
        {
            int userId = identityService.GetCurrentUserId();
            var query = db.Set<Notification>()
                .Where(n => n.Users.Any(u => u.UserId == userId));

            if (search.IsRead == false)
            {
                query = query.Where(n => n.Users.Any(u => u.UserId == userId && u.SeenAt == null));
            }

            query = query.OrderByDescending(n => n.CreatedAt);

            var pageSize = Math.Clamp(search.PageSize.GetValueOrDefault(10), 1, 100);
            var pageNumber = Math.Max(search.PageNumber.GetValueOrDefault(1), 1);
            var count = await query.CountAsync(cancellation);
            var items = await query
                                   .Include(n => n.Users.Where(u => u.UserId == userId))
                                   .Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync(cancellation);

            return new MyPagedList<NotificationResponse>(items.Select(MapToResponse).ToList(), count, pageNumber, pageSize);
        }

        public override async Task<NotificationResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            int userId = identityService.GetCurrentUserId();
            var entity = await db.Set<Notification>().AsNoTracking()
                .Include(n => n.Users.Where(u => u.UserId == userId))
                .FirstOrDefaultAsync(n => n.Id == id && n.Users.Any(u => u.UserId == userId), cancellationToken);

            return entity == null ? null : MapToResponse(entity);
        }

        protected override IQueryable<Notification> ApplyFilters(IQueryable<Notification> query, NotificationSearchObject search)
        {
            int userId = identityService.GetCurrentUserId();
            query = query.Include(n => n.Users.Where(u => u.UserId == userId))
                .Where(n => n.Users.Any(u => u.UserId == userId));
            
            if (search.IsRead == false)
            {
                query = query.Where(n => n.Users.Any(u => u.SeenAt == null));
            }

            query = query.OrderByDescending(n => n.CreatedAt);
            query = query.Take(30);

            return base.ApplyFilters(query, search);
        }

        protected override NotificationResponse MapToResponse(Notification entity)
        {
            var seenAt = entity.Users.FirstOrDefault()?.SeenAt;

            return new NotificationResponse
            {
                Id = entity.Id,
                Title = entity.Title,
                Body = entity.Body,
                ImageUrl = entity.ImageUrl,
                Data = entity.Data,
                CreatedAt = entity.CreatedAt,
                SeenAt = seenAt
            };
        }

        public async Task MarkAsSeen(int notificationId, CancellationToken cancellationToken)
        {
            int userId = identityService.GetCurrentUserId();

            var notification = await db.UserNotifications.Where(n => n.UserId == userId && n.NotificationId == notificationId).FirstOrDefaultAsync(cancellationToken);

            if (notification == null)
            {
                throw new UserException("Notification not found");
            }

            if(notification.SeenAt != null)
            {
                return;
            }

            notification.SeenAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            try
            {
                await cacheService.RemoveAsync($"notifications_user_{userId}");
                
                if (hubContext != null)
                {
                    var unreadCount = await db.UserNotifications.CountAsync(un => un.UserId == userId && un.SeenAt == null);
                    await hubContext.Clients.User(userId.ToString()).SendAsync("UnreadCountUpdated", new { unreadCount });
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove notification cache or broadcast SignalR for user {UserId}", userId);
            }
        }
        
        public async Task<int> GetUnreadCount(int userId, CancellationToken cancellationToken)
        {
            return await db.UserNotifications.CountAsync(un => un.UserId == userId && un.SeenAt == null, cancellationToken);
        }



        public async Task MarkAllAsSeen(CancellationToken cancellationToken)
        {
            int userId = identityService.GetCurrentUserId();

            var notifications = await db.UserNotifications.Where(n => n.UserId == userId && n.SeenAt == null).ToListAsync(cancellationToken);

            if (notifications == null)
            {
                throw new UserException("Notification not found");
            }

            foreach (var notification in notifications)
            {
                notification.SeenAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(cancellationToken);
            try
            {
                await cacheService.RemoveAsync($"notifications_user_{userId}");
                
                if (hubContext != null)
                {
                    await hubContext.Clients.User(userId.ToString()).SendAsync("UnreadCountUpdated", new { unreadCount = 0 });
                }

                await rmqh.PublishAsync(new SignalRNotificationEvent
                {
                    UserIds = new List<int> { userId },
                    MethodName = "UnreadCountUpdated",
                    Payload = new { unreadCount = 0 }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove notification cache or broadcast SignalR for user {UserId}", userId);
            }
        }

        public async Task DeleteUserNotification(int notificationId, CancellationToken cancellationToken)
        {
            int userId = identityService.GetCurrentUserId();

            var userNotification = await db.UserNotifications
                .FirstOrDefaultAsync(n => n.UserId == userId && n.NotificationId == notificationId, cancellationToken);

            if (userNotification == null)
                throw new UserException("Notification not found");

            db.UserNotifications.Remove(userNotification);
            await db.SaveChangesAsync(cancellationToken);

            try
            {
                await cacheService.RemoveAsync($"notifications_user_{userId}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove notification cache for user {UserId}", userId);
            }
        }

        public async Task DeleteAllUserNotifications(CancellationToken cancellationToken)
        {
            int userId = identityService.GetCurrentUserId();

            var userNotifications = await db.UserNotifications
                .Where(n => n.UserId == userId)
                .ToListAsync(cancellationToken);

            if (userNotifications.Any())
            {
                db.UserNotifications.RemoveRange(userNotifications);
                await db.SaveChangesAsync(cancellationToken);
            }

            try
            {
                await cacheService.RemoveAsync($"notifications_user_{userId}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove notification cache for user {UserId}", userId);
            }
        }
    }
}





