using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;

namespace SmartTrack.Services
{
    public class SmartTrackNotificationBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory
            _scopeFactory;

        private readonly ILogger<
            SmartTrackNotificationBackgroundService>
            _logger;


        public SmartTrackNotificationBackgroundService(
            IServiceScopeFactory scopeFactory,

            ILogger<
                SmartTrackNotificationBackgroundService>
                logger)
        {
            _scopeFactory =
                scopeFactory;

            _logger =
                logger;
        }


        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "SmartTrack notification processing failed.");
                }


                // Check every 30 minutes

                await Task.Delay(
                    TimeSpan.FromMinutes(30),
                    stoppingToken);
            }
        }


        private async Task
            ProcessNotificationsAsync()
        {
            using var scope =
                _scopeFactory
                    .CreateScope();


            var context =
                scope.ServiceProvider
                    .GetRequiredService<
                        ApplicationDbContext>();


            var emailService =
                scope.ServiceProvider
                    .GetRequiredService<
                        SmartTrackEmailService>();


            var notifications =
                await context
                    .SmartTrackNotifications
                    .Include(x => x.User)
                    .Where(x =>
                        !x.EmailSent &&
                        !x.IsRead &&
                        (
                            x.NotificationType ==
                                "PURCHASE_DUE"
                            ||
                            x.NotificationType ==
                                "PURCHASE_SOON"
                        ))
                    .OrderBy(
                        x => x.CreatedOn)
                    .Take(50)
                    .ToListAsync();


            foreach (var notification
                in notifications)
            {
                try
                {
                    var email =
                        notification.User.Email;


                    if (string.IsNullOrWhiteSpace(email))
                    {
                        continue;
                    }


                    var name =
                        notification.User.UserName
                        ?? "SmartTrack User";


                    await emailService
                        .SendPurchaseReminderAsync(
                            email,
                            name,
                            notification.ProductName,
                            notification.Message);


                    notification.EmailSent =
                        true;

                    notification.EmailSentOn =
                        DateTime.UtcNow;


                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to send SmartTrack email for notification {NotificationId}",
                        notification.NotificationId);
                }
            }
        }
    }
}