using System.ComponentModel.DataAnnotations;

namespace SmartTrack.Models
{
    public class HouseHoldDetails : CommonModel
    {
        [Key]
        public Guid HouseHoldId { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 3)]
        public string HouseHoldName { get; set; }

        [Range(1, 50)]
        public int TotalMembers { get; set; }

        public ICollection<UserHouseHoldDetails> UserHouseHolds { get; set; }
    }
}