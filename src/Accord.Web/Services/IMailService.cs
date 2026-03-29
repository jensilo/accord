namespace Accord.Web.Services;

public interface IMailService
{
    Task SendMagicLink(string email, string link);
}
