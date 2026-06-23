using System.ComponentModel.DataAnnotations;

namespace SmartTrack.ViewModel
{
    public class VerifyOtpViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Otp { get; set; }
    }
}
