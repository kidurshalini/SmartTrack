using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTrack.Models
{
    public class UserHouseHoldDetails : CommonModel
    {
        [Key]
        public Guid UserHouseHoldId { get; set; }


        [Required]
        public string UserId { get; set; }


        [Required]
        public Guid HouseHoldId { get; set; }


        // Foreign Keys
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }


     
        [ForeignKey(nameof(HouseHoldId))]
        public HouseHoldDetails HouseHold { get; set; }

    }
}