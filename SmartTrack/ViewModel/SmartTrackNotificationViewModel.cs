using System.ComponentModel.DataAnnotations;

namespace SmartTrack.ViewModel
{
    public class SmartTrackNotificationViewModel
    {

        public Guid NotificationId { get; set; }

        public string ProductName { get; set; }

        public string NotificationType { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedOn { get; set; }


    }
}
