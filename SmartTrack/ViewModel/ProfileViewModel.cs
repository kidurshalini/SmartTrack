using System.ComponentModel.DataAnnotations;

namespace SmartTrack.ViewModel
{

    public class ProfileViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public Guid HouseHoldId { get; set; }
        public string HouseHoldName { get; set; }
        public int TotalMembers { get; set; }
        public string Password { get; set; }
        public List<MemberViewModel> Members { get; set; }
        public AddMemberInputModel AddMemberInput { get; set; }


        public class MemberViewModel
        {
            public string UserId { get; set; }
            public string UserName { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Enter valid email address")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Phone number required")]
            [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "Phone number must be 10 to 15 digits")]
            public string PhoneNumber { get; set; }
            public string Role { get; set; }

            [Required(ErrorMessage = "Password required")]
            [MinLength(8, ErrorMessage = "Password minimum 8 characters")]
            [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).+$",
        ErrorMessage = "Password must contain uppercase, lowercase, number and special character"
    )]
            public string Password { get; set; }
        }

        public class AddMemberInputModel
        {
            [Required(ErrorMessage = "Username is required")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Enter valid email address")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Phone number required")]
            [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "Phone number must be 10 to 15 digits")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Role is required")]
            public string Role { get; set; }

            [Required(ErrorMessage = "Password required")]
            [MinLength(8, ErrorMessage = "Password minimum 8 characters")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).+$",
                ErrorMessage = "Password must contain uppercase, lowercase, number and special character")]
            public string Password { get; set; }
        }
    }
}



