using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTrack.Models
{
    public class ShoppingListItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShoppingListId { get; set; }

        [Required]
        [StringLength(200)]
        public string Product { get; set; }

        public decimal Quantity { get; set; }

        [StringLength(20)]
        public string Priority { get; set; }

        [StringLength(30)]
        public string RecommendationStatus { get; set; }

        public DateTime? ExpectedPurchaseDate { get; set; }

        public int DaysUntilPurchase { get; set; }

        public bool IsPurchased { get; set; } = false;

        public DateTime? PurchasedDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey(nameof(ShoppingListId))]
        public virtual ShoppingList ShoppingList { get; set; }
    }
}