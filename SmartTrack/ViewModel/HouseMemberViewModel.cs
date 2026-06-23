using System.ComponentModel.DataAnnotations;

namespace SmartTrack.ViewModels
{
    public class HouseMemberViewModel
    {


        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }


        [EmailAddress(ErrorMessage = "Enter valid email")]
        public string EmailId { get; set; }


        [Phone(ErrorMessage = "Enter valid phone number")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone number should be 10-15 digits")]
        public string PhoneNumber { get; set; }


      
        [MinLength(8, ErrorMessage = "Password minimum 8 characters")]
        [RegularExpression(
     @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).+$",
     ErrorMessage = "Password must contain uppercase, lowercase, number and special character"
 )]
        public string Password { get; set; }


        public string Role { get; set; }

    }
}