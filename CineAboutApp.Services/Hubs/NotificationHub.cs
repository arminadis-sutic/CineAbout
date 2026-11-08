using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CineAboutApp.Services.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HubCallerContext>> UserConnections = new();

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var connections = UserConnections.GetOrAdd(userId, _ => new ConcurrentDictionary<string, HubCallerContext>());
                connections.TryAdd(Context.ConnectionId, Context);
                _logger.LogInformation("User {UserId} connected to NotificationHub with ConnectionId {ConnectionId}", userId, Context.ConnectionId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                if (UserConnections.TryGetValue(userId, out var connections))
                {
                    connections.TryRemove(Context.ConnectionId, out _);
                    if (connections.IsEmpty)
                    {
                        UserConnections.TryRemove(userId, out _);
                    }
                }
                _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public static void DisconnectUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) || !UserConnections.TryGetValue(userId, out var connections))
                return;

            foreach (var context in connections.Values)
                context.Abort();
        }

        public static IReadOnlyList<string> GetConnectionsForUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Array.Empty<string>();

            return UserConnections.TryGetValue(userId, out var connections)
                ? connections.Keys.ToList()
                : Array.Empty<string>();
        }
    }
}
