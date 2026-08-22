using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartTrack.Models
{
    public class ShoppingList
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public string Status { get; set; } = "ACTIVE";

        public DateTime? CompletedDate { get; set; }

        // Navigation property
        public virtual ICollection<ShoppingListItem> Items { get; set; }
            = new List<ShoppingListItem>();
    }
}