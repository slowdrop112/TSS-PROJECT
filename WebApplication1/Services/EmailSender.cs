using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Uniflow.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptionsSnapshot<EmailSettings> emailSettings, ILogger<EmailSender> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Log for debugging
            _logger.LogInformation($"Sending email to {email} via {_emailSettings.SmtpServer}:{_emailSettings.Port}");
            _logger.LogInformation($"Username: {_emailSettings.Username}");

            try
            {
                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.Port))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email sent successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email. Check credentials/2FA.");
                _logger.LogWarning("--- EMAIL SENDING FAILED - FALLBACK LOGGING ---");
                _logger.LogWarning($"To: {email}");
                _logger.LogWarning($"Subject: {subject}");
                _logger.LogWarning($"Body: {htmlMessage}");
                _logger.LogWarning("-----------------------------------------------");
            }
        }


    }
}
