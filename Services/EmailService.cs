using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace ClothingStore.Services;

public class EmailService(
    IConfiguration configuration,
    ILogger<EmailService> logger) : IEmailService
{
    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var smtpServer = configuration["EmailSettings:SmtpServer"];
        var smtpPortStr = configuration["EmailSettings:SmtpPort"];
        var senderName = configuration["EmailSettings:SenderName"] ?? "WearWhatever";
        var senderEmail = configuration["EmailSettings:SenderEmail"];
        var smtpUsername = configuration["EmailSettings:SmtpUsername"];
        var smtpPassword = configuration["EmailSettings:SmtpPassword"];

        if (string.IsNullOrWhiteSpace(smtpServer) || smtpServer == "smtp.example.com" || smtpPassword == "YOUR_APP_PASSWORD")
        {
            // Development mode: just log to console
            logger.LogWarning("==========================================================================");
            logger.LogWarning("SMTP is not configured properly (missing real password)! Logging email content to console instead.");
            logger.LogWarning("To: {ToEmail}", toEmail);
            logger.LogWarning("Subject: {Subject}", subject);
            logger.LogWarning("Message: \n{Message}", message);
            logger.LogWarning("==========================================================================");
            return;
        }

        try
        {
            int.TryParse(smtpPortStr, out int smtpPort);

            using var client = new SmtpClient(smtpServer, smtpPort > 0 ? smtpPort : 587);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail!, senderName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
            logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {ToEmail}. Fallback logging content below:", toEmail);
            logger.LogWarning("--- EMAIL FALLBACK ---");
            logger.LogWarning("To: {ToEmail}", toEmail);
            logger.LogWarning("Subject: {Subject}", subject);
            logger.LogWarning("Message: \n{Message}", message);
            logger.LogWarning("----------------------");
        }
    }
}
