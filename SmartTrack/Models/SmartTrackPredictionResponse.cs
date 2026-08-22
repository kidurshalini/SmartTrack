using System.Text.Json.Serialization;

namespace SmartTrack.Models
{
    public class SmartTrackPredictionResponse
    {
        [JsonPropertyName("product")]
        public string? Product { get; set; }


        [JsonPropertyName("records_used")]
        public int RecordsUsed { get; set; }


        [JsonPropertyName("data_source")]
        public string? DataSource { get; set; }


        [JsonPropertyName("normal_consumption")]
        public double? NormalConsumption { get; set; }


        [JsonPropertyName("recent_consumption")]
        public double? RecentConsumption { get; set; }


        [JsonPropertyName("adaptive_consumption")]
        public double? AdaptiveConsumption { get; set; }


        [JsonPropertyName("normal_interval_days")]
        public double? NormalIntervalDays { get; set; }


        [JsonPropertyName("recent_interval_days")]
        public double? RecentIntervalDays { get; set; }


        [JsonPropertyName("adaptive_interval_days")]
        public double? AdaptiveIntervalDays { get; set; }


        [JsonPropertyName("adjustment")]
        public string? Adjustment { get; set; }


        [JsonPropertyName("adjustment_factor")]
        public double? AdjustmentFactor { get; set; }


        [JsonPropertyName("adjusted_interval_days")]
        public double? AdjustedIntervalDays { get; set; }


        [JsonPropertyName("latest_quantity")]
        public double? LatestQuantity { get; set; }


        [JsonPropertyName("last_purchase_date")]
        public string? LastPurchaseDate { get; set; }


        [JsonPropertyName("expected_purchase_date")]
        public string? ExpectedPurchaseDate { get; set; }


        [JsonPropertyName("days_until_purchase")]
        public int? DaysUntilPurchase { get; set; }


        [JsonPropertyName("status")]
        public string? Status { get; set; }


        [JsonPropertyName("recommendation")]
        public string? Recommendation { get; set; }


        [JsonPropertyName("anomaly")]
        public bool Anomaly { get; set; }


        [JsonPropertyName("anomaly_status")]
        public string? AnomalyStatus { get; set; }


        [JsonPropertyName("anomaly_score")]
        public double? AnomalyScore { get; set; }
    }
}