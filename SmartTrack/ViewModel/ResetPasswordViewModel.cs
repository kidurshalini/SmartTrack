using System.ComponentModel.DataAnnotations;

namespace SmartTrack.ViewModel
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}
