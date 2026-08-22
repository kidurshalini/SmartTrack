using System.Globalization;
using System.Text.Json.Serialization;
using SmartTrack.ViewModel;

namespace SmartTrack.Models
{
    public class OCRResponseModel
    {
        public List<ReceiptItemViewModel> Items { get; set; }
            = new List<ReceiptItemViewModel>();


        public string RawText { get; set; }


        // =====================================================
        // JSON DATE
        // =====================================================

        [JsonPropertyName("date")]
        public string DateString { get; set; }


        // =====================================================
        // C# DATE
        // =====================================================

        [JsonIgnore]
        public DateTime? Date
        {
            get
            {
                if (string.IsNullOrWhiteSpace(
                    DateString))
                {
                    return null;
                }


                if (DateTime.TryParseExact(
                    DateString,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedDate))
                {
                    return parsedDate;
                }


                return null;
            }
        }


        // =====================================================
        // TOTAL
        // =====================================================

        [JsonIgnore]
        public decimal TotalAmount
        {
            get
            {
                return Items?.Sum(
                    x => x.Price
                ) ?? 0;
            }
        }
    }
}