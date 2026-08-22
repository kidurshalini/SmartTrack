using SmartTrack.ViewModel;

namespace SmartTrack.Models
{
    // =========================================================
    // MAIN DASHBOARD VIEW MODEL
    // =========================================================
    public class SmartTrackDashboardViewModel
    {
        // =====================================================
        // USER
        // =====================================================

        public string UserName { get; set; } = string.Empty;


        // =====================================================
        // DASHBOARD COUNTS
        // =====================================================

        public int DueNowCount { get; set; }

        public int DueSoonCount { get; set; }

        public int UpcomingCount { get; set; }

        public int StockGettingLowCount { get; set; }

        public int AnomalyCount { get; set; }


        // =====================================================
        // PURCHASE RECOMMENDATIONS
        // =====================================================

        public List<PurchaseRecommendationViewModel>
            PurchaseRecommendations
        {
            get;
            set;
        } = new();


        // =====================================================
        // STOCK ITEMS
        // =====================================================

        public List<StockStatusViewModel>
            StockItems
        {
            get;
            set;
        } = new();


        // =====================================================
        // NOTIFICATIONS
        // =====================================================

        public List<SmartTrackNotificationViewModel>
            Notifications
        {
            get;
            set;
        } = new();


        // =====================================================
        // RECENT PURCHASES
        // =====================================================

        public List<RecentPurchaseViewModel>
            RecentPurchases
        {
            get;
            set;
        } = new();
    }


    // =========================================================
    // PURCHASE RECOMMENDATION VIEW MODEL
    // =========================================================

    public class PurchaseRecommendationViewModel
    {
        public string Product { get; set; } = string.Empty;

        public double LatestQuantity { get; set; }

        public string LastPurchaseDate { get; set; } = string.Empty;

        public string ExpectedPurchaseDate { get; set; } = string.Empty;

        public int DaysUntilPurchase { get; set; }

        public string Status { get; set; } = "NORMAL";

        public string Recommendation { get; set; } = string.Empty;

        public bool Anomaly { get; set; }

        public string AnomalyStatus { get; set; } = "NORMAL";

        public double AnomalyScore { get; set; }

        public string Priority { get; set; } = "NORMAL";


        // Optional values useful for the UI
        public double NormalConsumption { get; set; }

        public double RecentConsumption { get; set; }

        public double AdaptiveConsumption { get; set; }

        public double NormalIntervalDays { get; set; }

        public double RecentIntervalDays { get; set; }

        public double AdaptiveIntervalDays { get; set; }

        public string Adjustment { get; set; } = string.Empty;

        public double AdjustmentFactor { get; set; }
    }


    // =========================================================
    // RECENT PURCHASE VIEW MODEL
    // =========================================================

    public class RecentPurchaseViewModel
    {
        public string Product { get; set; } = string.Empty;

        public double Quantity { get; set; }

        public string PurchaseDate { get; set; } = string.Empty;

        public double UnitPrice { get; set; }

        public double TotalPrice { get; set; }

        public string UserId { get; set; } = string.Empty;
    }


    // =========================================================
    // STOCK STATUS VIEW MODEL
    // =========================================================

    public class StockStatusViewModel
    {
        public string Product { get; set; } = string.Empty;

        public double LatestQuantity { get; set; }

        public double AdaptiveConsumption { get; set; }

        public double AdaptiveIntervalDays { get; set; }

        public int DaysUntilPurchase { get; set; }

        public string StockStatus { get; set; } = "OK";

        public string StatusClass { get; set; } = "success";

        public string Priority { get; set; } = "NORMAL";

        public double CurrentStock { get; set; }

        public double NormalDailyConsumption { get; set; }

        public string LastAdjustmentType { get; set; } = "NORMAL";
    }
}