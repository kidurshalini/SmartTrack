namespace SmartTrack.Models
{
    public class SmartTrackPurchaseHistoryDto
    {
        public string ProductName { get; set; } = string.Empty;

        public double Quantity { get; set; }

        public string PurchaseDate { get; set; } = string.Empty;

        public double UnitPrice { get; set; }

        public double TotalPrice { get; set; }

        public string Category { get; set; } = "Unknown";

        public string UserId { get; set; } = string.Empty;

        public int ReceiptId { get; set; }
    }
}