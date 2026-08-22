using System.ComponentModel.DataAnnotations;

namespace SmartTrack.ViewModels
{
    public class SmartTrackPredictionViewModel
    {
        [Required]
        [Display(Name = "Product")]
        public string ProductName { get; set; } = string.Empty;


        // =====================================================
        // AI / PRODUCT INFORMATION
        // =====================================================

        public double CurrentStock { get; set; }

        public double AverageDailyUsage { get; set; }

        public double LastPurchaseQuantity { get; set; }

        public double DaysSinceLastPurchase { get; set; }


        // =====================================================
        // PREDICTION RESULT
        // =====================================================

        public bool HasPrediction { get; set; }

        public double PredictedDaysUntilPurchase { get; set; }

        public string StockStatus { get; set; }
            = string.Empty;

        public string StatusClass { get; set; }
            = "secondary";

        public string Recommendation { get; set; }
            = string.Empty;
    }
}