using System.ComponentModel.DataAnnotations;

namespace SmartTrack.Models
{
    public class CommonModel
    {
     
        [Required]
        public string CreatedBy { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [Required]
        [RegularExpression("Active|Inactive")]
        public string RecordStatus { get; set; } = "Active";
    }

}
