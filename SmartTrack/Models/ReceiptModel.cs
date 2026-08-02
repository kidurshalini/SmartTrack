using System.ComponentModel.DataAnnotations;

namespace SmartTrack.Models
{
    public class ReceiptModel : CommonModel
    {
        [Key]
        public int ReceiptId { get; set; }


        // Identity User Id is string by default
        public string UserId { get; set; }


        public DateTime PurchaseDate { get; set; }


        public string StoreName { get; set; } = string.Empty;


        public decimal TotalAmount { get; set; }


        // Navigation Property
        public ApplicationUser User { get; set; }


        public ICollection<ReceiptItemModel> ReceiptItems { get; set; }
            = new List<ReceiptItemModel>();
    }
}