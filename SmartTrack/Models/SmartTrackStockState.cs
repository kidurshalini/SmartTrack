using System;
using System.ComponentModel.DataAnnotations;

namespace SmartTrack.Models
{
    public class SmartTrackStockState
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public Guid HouseholdId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public decimal CurrentStock { get; set; }

        public decimal LastPurchaseQuantity { get; set; }

        public DateTime LastPurchaseDate { get; set; }

        public decimal NormalDailyConsumption { get; set; }

        public decimal AdaptiveConsumption { get; set; }

        public DateTime LastProcessedDate { get; set; }

        public string LastAdjustmentType { get; set; } = "NORMAL";

        public DateTime? LastAdjustmentDate { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}