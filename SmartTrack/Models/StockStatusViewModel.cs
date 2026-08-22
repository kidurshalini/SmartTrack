namespace SmartTrack.ViewModels
{
    public class StockStatusViewModel
    {
        public string Product { get; set; } = "";

        public double LatestQuantity { get; set; }

        public double CurrentStock { get; set; }

        public double NormalDailyConsumption { get; set; }

        public double AdaptiveConsumption { get; set; }

        public double AdaptiveIntervalDays { get; set; }

        public int DaysUntilPurchase { get; set; }

        public string StockStatus { get; set; } = "";

        public string StatusClass { get; set; } = "";

        public string Priority { get; set; } = "";

        public string LastAdjustmentType { get; set; } = "";
    }
}