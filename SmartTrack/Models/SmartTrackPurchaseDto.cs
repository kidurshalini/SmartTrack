namespace SmartTrack.Models
{
    public class SmartTrackPurchaseDto
    {
        public string ProductName { get; set; } = string.Empty;

        public double Quantity { get; set; }

        public double UnitPrice { get; set; }

        public double TotalPrice { get; set; }

        public DateTime PurchaseDate { get; set; }
    }
}