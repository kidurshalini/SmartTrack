public class StockStatusViewModel
{
    public string Product { get; set; }
        = string.Empty;

    // Latest quantity purchased
    public double LatestQuantity { get; set; }

    // REAL calculated current stock
    public double CurrentStock { get; set; }

    public double NormalDailyConsumption { get; set; }

    public double AdaptiveConsumption { get; set; }

    public double AdaptiveIntervalDays { get; set; }

    public int DaysUntilPurchase { get; set; }

    public string StockStatus { get; set; }
        = "OK";

    public string StatusClass { get; set; }
        = "success";

    public string Priority { get; set; }
        = "NORMAL";

    public string LastAdjustmentType { get; set; }
        = "NORMAL";
}