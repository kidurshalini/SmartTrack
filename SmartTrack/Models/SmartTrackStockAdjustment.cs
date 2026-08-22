namespace SmartTrack.Models
{
    public class SmartTrackStockAdjustment
    {
        public int Id { get; set; }

        public Guid HouseholdId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public DateTime AdjustmentDate { get; set; }

        // HIGH / MEDIUM / LOW / UNUSED
        public string AdjustmentType { get; set; } = "MEDIUM";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}