using CineAboutApp.Models.Mail;

namespace CineAboutApp.Interfaces.General
{
    public interface IMailService
    {
        public Task Send(MailData data);
    }
}
