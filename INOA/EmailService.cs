using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    private readonly Config _config;

    public EmailService(Config config)
    {
        _config = config;
    }

    public async Task SendAlertAsync(string alertType, string stockSymbol, double currentPrice, double threshold)
    {
        var subject = $"ALERT: {alertType} for {stockSymbol}";
        var body = $"Current price: R$ {currentPrice:0.00} - {(alertType == "SELL" ? "above" : "below")} threshold R$ {threshold:0.00}";

        try
        {
            using var smtpClient = new SmtpClient(_config.Smtp.Host, _config.Smtp.Port)
            {
                Credentials = new NetworkCredential(_config.Smtp.Username, _config.Smtp.Password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage(_config.Smtp.Username, _config.EmailDestination, subject, body);
            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (SmtpException ex)
        {
            Console.WriteLine($"SMTP error: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Email sending failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected email error: {ex.Message}");
        }
    }
}
