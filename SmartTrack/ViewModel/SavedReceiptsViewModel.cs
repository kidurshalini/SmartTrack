namespace SmartTrack.ViewModels
{
    public class SavedReceiptsViewModel
    {
        public int HouseholdId { get; set; }
        public string HouseholdName { get; set; } = "Household";
        public List<ReceiptListViewModel> Receipts { get; set; }
            = new List<ReceiptListViewModel>();

        public List<SavedReceiptItemViewModel> ReceiptItems { get; set; }
            = new List<SavedReceiptItemViewModel>();
    }


    public class SavedReceiptItemViewModel
    {
        public int Id { get; set; }

        public int ReceiptId { get; set; }

        public string ItemName { get; set; }

        public decimal Quantity { get; set; }

        public string Unit { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime PurchaseDate { get; set; }
    }
}