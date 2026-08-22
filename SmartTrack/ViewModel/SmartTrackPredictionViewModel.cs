using SmartTrack.ViewModel;

namespace SmartTrack.Models
{
    public class SmartTrackDashboardViewModel
    {
        public string UserName { get; set; }

        public int DueNowCount { get; set; }

        public int DueSoonCount { get; set; }

        public int UpcomingCount { get; set; }

        public int StockGettingLowCount { get; set; }

        public int AnomalyCount { get; set; }

        public List<PurchaseRecommendationViewModel>
            PurchaseRecommendations
        { get; set; }
            = new();

        public List<StockStatusViewModel>
            StockItems
        { get; set; }
            = new();

        public List<SmartTrackNotificationViewModel>
            Notifications
        { get; set; }
            = new();

        public List<RecentPurchaseViewModel>
            RecentPurchases
        { get; set; }
            = new();
    }


    public class PurchaseRecommendationViewModel
    {
        public string Product { get; set; }

        public double LatestQuantity { get; set; }

        public string LastPurchaseDate { get; set; }

        public string ExpectedPurchaseDate { get; set; }

        public int DaysUntilPurchase { get; set; }

        public string Status { get; set; }

        public string Recommendation { get; set; }

        public bool Anomaly { get; set; }

        public string AnomalyStatus { get; set; }

        public double? AnomalyScore { get; set; }

        public string Priority { get; set; }
    }


    public class StockStatusViewModel
    {
        public string Product { get; set; }

        public double LatestQuantity { get; set; }

        public double AdaptiveConsumption { get; set; }

        public double AdaptiveIntervalDays { get; set; }

        public int DaysUntilPurchase { get; set; }

        public string StockStatus { get; set; }

        public string StatusClass { get; set; }
    }


    public class RecentPurchaseViewModel
    {
        public string Product { get; set; }

        public double Quantity { get; set; }

        public string PurchaseDate { get; set; }

        public double UnitPrice { get; set; }

        public double TotalPrice { get; set; }

        public string UserId { get; set; }
    }
}