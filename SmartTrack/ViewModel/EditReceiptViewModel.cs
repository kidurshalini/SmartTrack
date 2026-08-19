namespace SmartTrack.ViewModels
{
    public class EditReceiptViewModel
    {
        public int ReceiptId { get; set; }

        public DateTime PurchaseDate { get; set; }

        public decimal TotalAmount { get; set; }

        public List<EditReceiptItemViewModel> Items { get; set; }
            = new List<EditReceiptItemViewModel>();
    }

    public class EditReceiptItemViewModel
    {
        public int ReceiptItemId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Unit { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }
    }
}