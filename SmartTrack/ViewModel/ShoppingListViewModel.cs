namespace SmartTrack.ViewModels
{
    public class ShoppingListViewModel
    {
        public int Id { get; set; }

        public string Status { get; set; } = "EMPTY";

        public DateTime? CreatedDate { get; set; }

        public int TotalItems { get; set; }

        public int PurchasedItems { get; set; }

        public int RemainingItems { get; set; }

        public List<ShoppingListItemViewModel> Items { get; set; }
            = new List<ShoppingListItemViewModel>();
    }


    public class ShoppingListItemViewModel
    {
        public int Id { get; set; }

        public string Product { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public string Priority { get; set; } = "NORMAL";

        public string RecommendationStatus { get; set; } = string.Empty;

        public DateTime? ExpectedPurchaseDate { get; set; }

        public int DaysUntilPurchase { get; set; }

        public bool IsPurchased { get; set; }

        public DateTime? PurchasedDate { get; set; }
    }
}