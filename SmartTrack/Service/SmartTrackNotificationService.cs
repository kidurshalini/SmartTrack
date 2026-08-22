using Microsoft.EntityFrameworkCore;
using SmartTrack.Models;

namespace SmartTrack.Services
{
    public class SmartTrackNotificationService
    {
        private readonly ApplicationDbContext _context;

        public SmartTrackNotificationService(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // GET HOUSEHOLD NOTIFICATIONS
        // =====================================================

        public async Task<
            List<SmartTrackNotification>>
            GetNotificationsAsync(
                string userId,
                Guid householdId)
        {
            return await _context
                .SmartTrackNotifications
                .Where(x =>
                    x.HouseHoldId == householdId &&
                    x.UserId == userId)
                .OrderByDescending(
                    x => x.CreatedOn)
                .Take(20)
                .ToListAsync();
        }


        // =====================================================
        // UNREAD COUNT
        // =====================================================

        public async Task<int>
            GetUnreadCountAsync(
                string userId,
                Guid householdId)
        {
            return await _context
                .SmartTrackNotifications
                .CountAsync(x =>
                    x.HouseHoldId == householdId &&
                    x.UserId == userId &&
                    !x.IsRead);
        }


        // =====================================================
        // CREATE NOTIFICATION
        // =====================================================

        public async Task<SmartTrackNotification>
            CreateNotificationAsync(
                string userId,
                Guid householdId,
                string productName,
                string type,
                string message)
        {
            var today = DateTime.UtcNow.Date;

            // Prevent duplicate notification
            var exists = await _context
                .SmartTrackNotifications
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.HouseHoldId == householdId &&
                    x.ProductName == productName &&
                    x.NotificationType == type &&
                    x.CreatedOn.Date == today);

            if (exists)
            {
                return null;
            }

            var notification =
                new SmartTrackNotification
                {
                    NotificationId =
                        Guid.NewGuid(),

                    HouseHoldId =
                        householdId,

                    UserId =
                        userId,

                    ProductName =
                        productName,

                    NotificationType =
                        type,

                    Message =
                        message,

                    Status =
                        "NEW",

                    IsRead =
                        false,

                    EmailSent =
                        false,

                    CreatedOn =
                        DateTime.UtcNow
                };

            _context
                .SmartTrackNotifications
                .Add(notification);

            await _context.SaveChangesAsync();

            return notification;
        }


        // =====================================================
        // MARK AS READ
        // =====================================================

        public async Task<bool>
            MarkAsReadAsync(
                Guid notificationId,
                string userId)
        {
            var notification =
                await _context
                    .SmartTrackNotifications
                    .FirstOrDefaultAsync(x =>
                        x.NotificationId ==
                            notificationId &&
                        x.UserId ==
                            userId);

            if (notification == null)
            {
                return false;
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}