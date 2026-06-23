using System.ComponentModel.DataAnnotations;

namespace SmartTrack.ViewModels
{
    public class HouseholdRegisterViewModel
    {

        [Required(ErrorMessage = "House name is required")]
        public string HouseHoldName { get; set; }


        [Required(ErrorMessage = "Total members required")]
        [Range(1, 100, ErrorMessage = "Members should be between 1 and 50")]
        public int TotalMembers { get; set; }



        [Required(ErrorMessage = "Username is required")]
        public string OwnerUserName { get; set; }



        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter valid email address")]
        public string OwnerEmailId { get; set; }



        [Required(ErrorMessage = "Phone number required")]
        [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "Phone number must be 10 to 15 digits")]
        public string OwnerPhoneNumber { get; set; }


        [Required(ErrorMessage = "Password required")]
        [MinLength(8, ErrorMessage = "Password minimum 8 characters")]
        [RegularExpression(
     @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).+$",
     ErrorMessage = "Password must contain uppercase, lowercase, number and special character"
 )]
        public string OwnerPassword { get; set; }

        public List<HouseMemberViewModel>? Members { get; set; }

    }
}