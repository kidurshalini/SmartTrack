namespace SmartTrack.Models
{
    public class SmartTrackDailyUsage
    {
        public int Id { get; set; }

        public Guid HouseholdId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        // Date this usage belongs to
        public DateTime UsageDate { get; set; }

        // NORMAL / HIGH / MEDIUM / LOW / UNUSED
        public string UsageType { get; set; } = "NORMAL";

        // Example:
        // NORMAL = 1.0
        // HIGH   = 1.5
        // MEDIUM = 1.0
        // LOW    = 0.5
        // UNUSED = 0
        public decimal AdjustmentFactor { get; set; }

        // Normal daily usage before adjustment
        public decimal NormalUsage { get; set; }

        // Actual amount reduced for this day
        public decimal ActualUsage { get; set; }

        public decimal StockBefore { get; set; }

        public decimal StockAfter { get; set; }

        // true = no user entry, system automatically used NORMAL
        // false = user explicitly selected HIGH/MEDIUM/LOW/UNUSED
        public bool IsAutomatic { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}