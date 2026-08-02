using SmartTrack.ViewModel;

namespace SmartTrack.ViewModels
{
    public class SaveReceiptViewModel
    {
        public DateTime? Date { get; set; }

        public List<ReceiptItemViewModel> Items { get; set; }
    }
}