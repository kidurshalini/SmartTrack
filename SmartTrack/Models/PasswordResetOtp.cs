namespace SmartTrack.Models
{
    public class PasswordResetOtp
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public string OtpCode { get; set; }

        public DateTime ExpiryTime { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
