using SmartTrack.ViewModel;

namespace SmartTrack.Models
{
    public class OCRResponseModel
    {
        public List<ReceiptItemViewModel> Items { get; set; }

        public string RawText { get; set; }

        public DateTime? Date { get; set; }

        public decimal TotalAmount
        {
            get
            {
                return Items?.Sum(x => x.Price) ?? 0;
            }
        }
    }
}
