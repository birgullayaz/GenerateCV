using System.IO;
using System.Threading.Tasks;

namespace GenerateCV.Event
{
    public interface IEmailEvent
    {
        Task SendEmailAsync(string name, string email, Stream pdfStream);
    }
} 