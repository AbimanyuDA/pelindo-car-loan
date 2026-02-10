using System.Collections.Concurrent;
using System.Text.Json;

namespace PelindoCarLoan.API.Services
{
    /// <summary>
    /// Service for managing real-time notifications via Server-Sent Events (SSE)
    /// </summary>
    public interface INotificationService
    {
        void Subscribe(string userId, string role, HttpResponse response);
        void Unsubscribe(string userId);
        Task NotifyApprovalPendingAsync(int level, string approvalType = "new");
        Task NotifyLoanRequestStatusChangeAsync(int requestId, string status, string userId);
        Task NotifyNewLoanRequestAsync(string pemohonId, string pemohonName);
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly ConcurrentDictionary<string, (HttpResponse response, StreamWriter writer)> _subscribers;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
            _subscribers = new ConcurrentDictionary<string, (HttpResponse, StreamWriter)>();
        }

        public void Subscribe(string userId, string role, HttpResponse response)
        {
            try
            {
                var writer = new StreamWriter(response.Body);
                _subscribers.TryAdd(userId, (response, writer));
                _logger.LogInformation($"User {userId} (Role: {role}) subscribed to notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error subscribing user {userId}: {ex.Message}");
            }
        }

        public void Unsubscribe(string userId)
        {
            if (_subscribers.TryRemove(userId, out var subscriber))
            {
                try
                {
                    // Don't dispose synchronously - let HttpResponse handle cleanup
                    subscriber.writer?.FlushAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error flushing writer for user {userId}: {ex.Message}");
                }
                _logger.LogInformation($"User {userId} unsubscribed from notifications");
            }
        }

        public async Task NotifyApprovalPendingAsync(int level, string approvalType = "new")
        {
            try
            {
                var roleFilter = level == 1 ? "PIC_APPROVAL_L1" : "PIC_APPROVAL_L2";
                var message = new
                {
                    type = "APPROVAL_UPDATE",
                    level = level,
                    approvalType = approvalType,
                    timestamp = DateTime.Now,
                    message = $"New approval pending for Level {level}"
                };

                await BroadcastToRoleAsync(roleFilter, message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notifying approvals: {ex.Message}");
            }
        }

        public async Task NotifyLoanRequestStatusChangeAsync(int requestId, string status, string userId)
        {
            try
            {
                var message = new
                {
                    type = "LOAN_REQUEST_STATUS_CHANGE",
                    requestId = requestId,
                    status = status,
                    timestamp = DateTime.Now,
                    message = $"Your loan request status changed to {status}"
                };

                await SendToUserAsync(userId, message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notifying loan request status: {ex.Message}");
            }
        }

        public async Task NotifyNewLoanRequestAsync(string pemohonId, string pemohonName)
        {
            try
            {
                var message = new
                {
                    type = "NEW_LOAN_REQUEST",
                    pemohonId = pemohonId,
                    pemohonName = pemohonName,
                    timestamp = DateTime.Now,
                    message = $"New loan request from {pemohonName}"
                };

                // Notify approval L1
                await BroadcastToRoleAsync("PIC_APPROVAL_L1", message);
                await BroadcastToRoleAsync("ADMIN", message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notifying new loan request: {ex.Message}");
            }
        }

        private async Task BroadcastToRoleAsync(string role, object message)
        {
            var tasks = _subscribers.Values.Select(sub => SendSSEMessageAsync(sub.writer, message));
            await Task.WhenAll(tasks.ToList());
        }

        private async Task SendToUserAsync(string userId, object message)
        {
            if (_subscribers.TryGetValue(userId, out var subscriber))
            {
                await SendSSEMessageAsync(subscriber.writer, message);
            }
        }

        private async Task SendSSEMessageAsync(StreamWriter writer, object message)
        {
            try
            {
                var json = JsonSerializer.Serialize(message);
                await writer.WriteLineAsync($"data: {json}");
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending SSE message: {ex.Message}");
            }
        }
    }
}
