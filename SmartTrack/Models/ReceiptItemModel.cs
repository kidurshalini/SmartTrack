using System.ComponentModel.DataAnnotations;

namespace SmartTrack.Models
{
    public class ReceiptItemModel : CommonModel
    {
        [Key]
        public int ReceiptItemId { get; set; }


        public string ItemName { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public int Quantity { get; set; }

        public string? Unit { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

   
        // Foreign Key
        public int ReceiptId { get; set; }


        // Navigation Property
        public ReceiptModel Receipt { get; set; }
    }
}