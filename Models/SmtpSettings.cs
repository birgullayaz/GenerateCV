namespace GenerateCV.Models
{
    public class SmtpSettings
    {
        public string Server { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
    }
} 