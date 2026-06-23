using System.ComponentModel.DataAnnotations;

namespace SmartTrack.ViewModel
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
