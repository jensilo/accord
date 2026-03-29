using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Accord.Web.Services;

public class MailOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool UseTls { get; set; } = false;
    public string From { get; set; } = "accord@localhost";
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class MailService(IOptions<MailOptions> options) : IMailService
{
    public async Task SendMagicLink(string email, string link)
    {
        var o = options.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(o.From));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Your sign-in link for Accord";
        message.Body = new TextPart("html")
        {
            Text = $"""
                    <p>Click the link below to sign in to Accord. The link expires in 15 minutes.</p>
                    <p><a href="{link}">{link}</a></p>
                    <p>If you did not request this, you can ignore this email.</p>
                    """
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(o.Host, o.Port, o.UseTls ? SecureSocketOptions.Auto : SecureSocketOptions.None);

        if (!string.IsNullOrEmpty(o.Username))
            await client.AuthenticateAsync(o.Username, o.Password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
