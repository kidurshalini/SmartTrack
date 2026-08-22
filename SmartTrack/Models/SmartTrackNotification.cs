using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTrack.Models
{
    public class SmartTrackNotification
    {
        [Key]
        public Guid NotificationId { get; set; }

        [Required]
        public Guid HouseHoldId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string ProductName { get; set; }

        [Required]
        public string NotificationType { get; set; }

        [Required]
        public string Message { get; set; }

        public string? Status { get; set; }

        public bool IsRead { get; set; }

        public bool EmailSent { get; set; }

        public DateTime? EmailSentOn { get; set; }

        public DateTime CreatedOn { get; set; }

        [ForeignKey(nameof(HouseHoldId))]
        public HouseHoldDetails HouseHold { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
    }
}