using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pick_To_Ride.Data;
using Pick_To_Ride.Models.Entities;

namespace Pick_To_Ride.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        private readonly SmtpSettings _smtpSettings;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public BaseController(ApplicationDbContext context, IOptions<SmtpSettings> smtpOptions)
        {
            _context = context;
            _smtpSettings = smtpOptions.Value;
        }

        protected async Task LogAuditAsync(string action, string entityName, string entityId, string performedBy, string details = "")
        {
            try
            {
                var log = new AuditLog
                {
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    PerformedBy = performedBy,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // never throw from audit logging - fail silently
            }
        }

        protected void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                using var client = new System.Net.Mail.SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    EnableSsl = _smtpSettings.UseSSL,
                    Credentials = new System.Net.NetworkCredential(_smtpSettings.Username, _smtpSettings.Password)
                };

                var mail = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(_smtpSettings.Username, "Pick To Ride"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                mail.To.Add(toEmail);

                client.Send(mail);
            }
            catch
            {
                // ignore mail errors (optionally log with audit)
            }
        }
    }
}
