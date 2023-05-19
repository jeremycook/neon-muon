using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net.Mail;

namespace WebBlazorServerApp.Areas.Identity;

public class IdentityEmailSender : IEmailSender
{
    private readonly IdentitySettings identitySettings;
    private readonly ILogger logger;

    public IdentityEmailSender(ILogger<IdentityEmailSender> logger, IdentitySettings identitySettings)
    {
        this.identitySettings = identitySettings;
        this.logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        await SendEmailAsync(toEmail, subject, message, CancellationToken.None);
    }

    public async Task SendEmailAsync(string recipient, string subject, string message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient()
        {
            Host = identitySettings.Host,
            Port = identitySettings.Port,
        };
        using var msg = new MailMessage()
        {
            From = new MailAddress(identitySettings.FromAddress),
            Subject = subject,
            IsBodyHtml = true,
            Body = message,
        };
        msg.To.Add(new MailAddress(recipient));

        try
        {
            await client.SendMailAsync(msg, cancellationToken);
            logger.LogInformation("Emailed {Subject} to {Recipient}", subject, recipient);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Failed to email {Subject} to {Recipient}", subject, recipient);
            throw;
        }
    }
}
