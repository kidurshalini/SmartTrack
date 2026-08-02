using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SmartTrack.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<UserHouseHoldDetails> UserHouseHolds { get; set; }
        public ICollection<ReceiptModel> Receipts { get; set; }
    }
}