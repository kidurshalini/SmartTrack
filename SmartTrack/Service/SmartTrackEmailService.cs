using System.Net;
using System.Net.Mail;

namespace SmartTrack.Services
{
    public class SmartTrackEmailService
    {
        private readonly IConfiguration _configuration;

        public SmartTrackEmailService(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public async Task SendPurchaseReminderAsync(
            string recipientEmail,
            string recipientName,
            string productName,
            string message)
        {
            var settings =
                _configuration
                    .GetSection("SmartTrackEmail");


            var host =
                settings["Host"];

            var port =
                int.Parse(
                    settings["Port"] ?? "587");

            var username =
                settings["Username"];

            var password =
                settings["Password"];

            var fromName =
                settings["FromName"]
                ?? "SmartTrack AI";


            using var mail =
                new MailMessage();

            mail.From =
                new MailAddress(
                    username,
                    fromName);

            mail.To.Add(
                recipientEmail);

            mail.Subject =
                $"SmartTrack Purchase Reminder - {productName}";

            mail.IsBodyHtml = true;

            mail.Body = $@"
<html>
<body>

<h2>SmartTrack AI</h2>

<p>Hello {WebUtility.HtmlEncode(recipientName)},</p>

<p>
SmartTrack has identified a purchase reminder
for your household.
</p>

<h3>{WebUtility.HtmlEncode(productName)}</h3>

<p>
{WebUtility.HtmlEncode(message)}
</p>

<p>
Please open SmartTrack to review the full
purchase recommendation.
</p>

<hr />

<p>
SmartTrack AI Household Assistant
</p>

</body>
</html>
";


            using var smtp =
                new SmtpClient(
                    host,
                    port);

            smtp.EnableSsl = true;

            smtp.Credentials =
                new NetworkCredential(
                    username,
                    password);

            await smtp.SendMailAsync(
                mail);
        }
    }
}