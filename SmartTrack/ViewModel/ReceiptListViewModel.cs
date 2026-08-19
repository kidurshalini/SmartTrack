namespace SmartTrack.ViewModels
{
    public class ReceiptListViewModel
    {
        public int Id { get; set; }

        public DateTime PurchaseDate { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedOn { get; set; }

        public string CreatedByName { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string ModifiedByName { get; set; }

        public int ItemCount { get; set; }
    }
}