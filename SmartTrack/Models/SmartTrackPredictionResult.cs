namespace SmartTrack.Models
{
    public class SmartTrackPredictionResult
    {
        public int PredictedDaysUntilPurchase { get; set; }

        public string StockStatus { get; set; } = "No Data";

        public double CurrentStock { get; set; }

        public double AverageDailyUsage { get; set; }

        public double LastPurchaseQuantity { get; set; }

        public int DaysSinceLastPurchase { get; set; }

        public string Recommendation { get; set; } = string.Empty;
    }
}