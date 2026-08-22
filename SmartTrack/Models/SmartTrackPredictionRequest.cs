using System.Text.Json.Serialization;

namespace SmartTrack.Models
{
    public class SmartTrackPredictionRequest
    {
        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("adjustment")]
        public string? Adjustment { get; set; }

        [JsonPropertyName("purchase_history")]
        public List<SmartTrackPurchaseHistoryItem> PurchaseHistory { get; set; }
            = new List<SmartTrackPurchaseHistoryItem>();
    }

    public class SmartTrackPurchaseHistoryItem
    {
        [JsonPropertyName("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("purchase_date")]
        public DateTime PurchaseDate { get; set; }

        [JsonPropertyName("unit_price")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = "Unknown";
    }
}