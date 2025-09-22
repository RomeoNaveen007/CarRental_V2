using Microsoft.Extensions.Options;
using Pick_To_Ride.Helpers;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using static Pick_To_Ride.Program;

namespace Pick_To_Ride.Helpers
{
    public class EmailHelper
    {
        private readonly SmtpSettings _cfg;
        public EmailHelper(IOptions<SmtpSettings> cfg)
        {
            _cfg = cfg.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var from = _cfg.Username ?? _cfg.Username;
            var message = new MailMessage(from, toEmail, subject, htmlBody) { IsBodyHtml = true };
            if (!string.IsNullOrEmpty(_cfg.Username))
                message.From = new MailAddress(from, _cfg.Username);

            using (var smtp = new SmtpClient(_cfg.Host, _cfg.Port))
            {
                smtp.EnableSsl = _cfg.UseSSL;
                smtp.Credentials = new NetworkCredential(_cfg.Username, _cfg.Password);
                await smtp.SendMailAsync(message);
            }
        }
    }
}
