
public class StockStatusViewModel
{
    public string Product { get; set; } = string.Empty;

    public double LatestQuantity { get; set; }

    public double AdaptiveConsumption { get; set; }

    public double AdaptiveIntervalDays { get; set; }

    public int DaysUntilPurchase { get; set; }

    public string StockStatus { get; set; } = "OK";

    public string StatusClass { get; set; } = "success";

    // IMPORTANT:
    // Your Dashboard.cshtml uses item.Priority.
    public string Priority { get; set; } = "NORMAL";
    public double CurrentStock { get; internal set; }
    public double NormalDailyConsumption { get; internal set; }
    public string LastAdjustmentType { get; internal set; }
}
