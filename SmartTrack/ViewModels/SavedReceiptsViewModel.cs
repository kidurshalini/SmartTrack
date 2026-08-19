using SmartTrack.ViewModels;

public class SavedReceiptsViewModel
{
    public int HouseholdId { get; set; }
    public string HouseholdName { get; set; }
    public List<ReceiptListViewModel> Receipts { get; set; }
    public List<SavedReceiptItemViewModel> ReceiptItems { get; set; }
}