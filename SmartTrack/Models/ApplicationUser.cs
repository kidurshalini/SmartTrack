using Microsoft.AspNetCore.Identity;

namespace SmartTrack.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<UserHouseHoldDetails> UserHouseHolds { get; set; }
    }
}