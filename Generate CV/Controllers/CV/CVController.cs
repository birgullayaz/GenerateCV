using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MailKit.Net.Smtp;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Elements;
using GenerateCV.Models;
using GenerateCV.Event;
using GenerateCV.Data;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using NpgsqlTypes;

namespace GenerateCV.Controllers
{
    public class CVController(
        IConfiguration configuration,
        CVDbContext context,
        IEmailEvent emailEvent,
        ILogger<CVController> logger) : Controller
    {
        static CVController()
        {
            // QuestPDF.Settings.License = LicenseType.Community;
        }

        private readonly IConfiguration _configuration = configuration;
        private readonly CVDbContext _context = context;
        private readonly IEmailEvent _emailEvent = emailEvent;
        private readonly ILogger<CVController> _logger = logger;

        [HttpGet]
        public IActionResult GenerateCV()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (!string.IsNullOrEmpty(userEmail))
            {
                ViewData["UserEmail"] = userEmail;
                _logger.LogInformation("Setting email in GenerateCV: {Email}", userEmail);
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPdf(
            [FromForm] string? name = null, 
            [FromForm] string? surname = null, 
            [FromForm] string? email = null, 
            [FromForm] string? telephone = null,
            [FromForm] string? skills = null,
            [FromForm] List<School>? schools = null,
            [FromForm] List<Experience>? experiences = null,
            [FromForm] IFormFile? photo = null)
        {
            try
            {
                _logger.LogInformation("Starting PDF generation");

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email))
                {
                    return BadRequest("Ad ve email alanları zorunludur.");
                }

                var cv = new CV
                {
                    Name = name,
                    Surname = surname,
                    Email = email,
                    Phone = telephone,
                    Skills = skills,
                    CreatedAt = DateTime.UtcNow
                };

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        page.Content().Column(column =>
                        {
                            column.Item().Text($"{cv.Name} {cv.Surname ?? string.Empty}").Bold().FontSize(20);
                            column.Item().Text(cv.Email ?? string.Empty);
                            if (!string.IsNullOrWhiteSpace(cv.Phone))
                            {
                                column.Item().Text($"Tel: {cv.Phone}");
                            }

                            if (!string.IsNullOrEmpty(cv.Skills))
                            {
                                column.Item().Text("Skills").Bold().FontSize(14);
                                column.Item().Text(cv.Skills);
                            }

                            if (schools?.Any() == true)
                            {
                                column.Item().Text("Education").Bold().FontSize(14);
                                foreach (var school in schools.Where(s => !string.IsNullOrEmpty(s.Name)))
                                {
                                    column.Item().Text(school.Name);
                                    if (!string.IsNullOrEmpty(school.Department))
                                        column.Item().Text(school.Department);
                                }
                            }

                            if (experiences?.Any() == true)
                            {
                                column.Item().Text("Experience").Bold().FontSize(14);
                                foreach (var exp in experiences.Where(e => !string.IsNullOrEmpty(e.Company)))
                                {
                                    column.Item().Text(exp.Company);
                                    if (!string.IsNullOrEmpty(exp.Year))
                                        column.Item().Text(exp.Year);
                                    if (!string.IsNullOrEmpty(exp.Details))
                                        column.Item().Text(exp.Details);
                                }
                            }
                        });
                    });
                });

                var pdfBytes = document.GeneratePdf();
                
                if (!string.IsNullOrEmpty(email))
                {
                    using var stream = new MemoryStream(pdfBytes);
                    await _emailEvent.SendEmailAsync(name, email, stream);
                }

                return File(pdfBytes, "application/pdf", $"CV_{name}_{surname}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF: {Message}", ex.Message);
                return StatusCode(500, $"PDF oluşturulurken bir hata oluştu: {ex.Message}");
            }
        }

        private async Task SendEmailAsync(string name, string surname, string email, byte[] pdfBytes)
        {
            try 
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SmtpUsername"]));
                message.To.Add(new MailboxAddress($"{name} {surname}", email));
                message.Subject = "CV'niz Hazır!";

                var builder = new BodyBuilder
                {
                    TextBody = $"Merhaba {name} {surname},\n\nCV'niz ekte yer almaktadır.\n\nSaygılarımızla,\nCV Maker"
                };
                builder.Attachments.Add($"CV_{name}_{surname}.pdf", pdfBytes);
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(emailSettings["SmtpServer"], 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(emailSettings["SmtpUsername"], emailSettings["SmtpPassword"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", email);

                // Admin'e bildirim gönder
                var adminMessage = new MimeMessage();
                adminMessage.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SmtpUsername"]));
                adminMessage.To.Add(new MailboxAddress("Admin", emailSettings["AdminEmail"]));
                adminMessage.Subject = "Yeni CV Oluşturuldu";

                var adminBuilder = new BodyBuilder
                {
                    TextBody = $"Yeni bir CV oluşturuldu!\n\n" +
                               $"Kullanıcı: {name} {surname}\n" +
                               $"Email: {email}\n" +
                               $"Tarih: {DateTime.Now:dd/MM/yyyy HH:mm}"
                };
                adminBuilder.Attachments.Add($"CV_{name}_{surname}.pdf", pdfBytes);
                adminMessage.Body = adminBuilder.ToMessageBody();

                using var adminClient = new SmtpClient();
                await adminClient.ConnectAsync(emailSettings["SmtpServer"], 587, MailKit.Security.SecureSocketOptions.StartTls);
                await adminClient.AuthenticateAsync(emailSettings["SmtpUsername"], emailSettings["SmtpPassword"]);
                await adminClient.SendAsync(adminMessage);
                await adminClient.DisconnectAsync(true);

                _logger.LogInformation("Admin notification sent successfully");
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send email to {Email}", email);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail(
            [FromForm] string name, 
            [FromForm] string surname, 
            [FromForm] string email, 
            [FromForm] IFormFile pdfFile)
        {
            try
            {
                if (pdfFile is null)
                {
                    return BadRequest("PDF file is required");
                }

                using var stream = pdfFile.OpenReadStream();
                await _emailEvent.SendEmailAsync($"{name} {surname}", email, stream);
                _logger.LogInformation("Email sent successfully to {Email}", email);

                return Ok("Email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return StatusCode(500, "Failed to send email");
            }
        }
    }
}
