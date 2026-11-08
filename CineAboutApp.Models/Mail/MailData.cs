using System.Net.Mail;
using System.Text;

namespace CineAboutApp.Models.Mail
{
    public class MailData
    {
        public string Subject { get; set; } = null!;
        public StringBuilder Body { get; set; } = null!;
        public string To { get; set; } = null!;
        public List<string> CC { get; set; } = new();
        public List<string> BCC { get; set; } = new();
        public MailPriority Priority { get; set; } = MailPriority.Normal;
        public bool IsBodyHtml { get; set; }
        public List<MailAttachment> Attachments { get; set; } = new();
    }

    public class MailAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
