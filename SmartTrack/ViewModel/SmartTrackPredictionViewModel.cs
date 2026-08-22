using System.ComponentModel.DataAnnotations;

namespace SmartTrack.ViewModels
{
    public class SmartTrackPredictionViewModel
    {
        [Required]
        [Display(Name = "Product")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0, 100000)]
        [Display(Name = "Current Stock")]
        public double CurrentStock { get; set; }

        [Required]
        [Range(0, 100000)]
        [Display(Name = "Average Daily Usage")]
        public double AverageDailyUsage { get; set; }

        [Required]
        [Range(0, 100000)]
        [Display(Name = "Last Purchase Quantity")]
        public double LastPurchaseQuantity { get; set; }

        [Required]
        [Range(0, 100000)]
        [Display(Name = "Days Since Last Purchase")]
        public double DaysSinceLastPurchase { get; set; }

        // Prediction result
        public bool HasPrediction { get; set; }

        public double PredictedDaysUntilPurchase { get; set; }

        public string StockStatus { get; set; } = string.Empty;

        public string StatusClass { get; set; } = "secondary";

        public string Recommendation { get; set; } = string.Empty;
    }
}