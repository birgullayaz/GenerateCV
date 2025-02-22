using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;

namespace GenerateCV.Event
{
    public class EmailEvent : IEmailEvent
    {
        private readonly IConfiguration _configuration;

        public EmailEvent(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string name, string email, Stream pdfStream)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SmtpUsername"]));
            message.To.Add(new MailboxAddress(name, email));
            message.Subject = "CV'niz Hazır!";

            var builder = new BodyBuilder
            {
                TextBody = $"Merhaba {name},\n\nCV'niz ekte yer almaktadır.\n\nSaygılarımızla,\nCV Maker"
            };

            using var ms = new MemoryStream();
            await pdfStream.CopyToAsync(ms);
            builder.Attachments.Add($"CV_{name}.pdf", ms.ToArray());
            
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(emailSettings["SmtpServer"], 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(emailSettings["SmtpUsername"], emailSettings["SmtpPassword"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
} 