using System.Net;
using System.Net.Mail;

public class EmailService
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

        using var smtpClient = new SmtpClient(_config.Smtp.Host, _config.Smtp.Port)
        {
            Credentials = new NetworkCredential(_config.Smtp.Username, _config.Smtp.Password),
            EnableSsl = true
        };

        var mailMessage = new MailMessage(_config.Smtp.Username, _config.EmailDestination, subject, body);
        await smtpClient.SendMailAsync(mailMessage);
    }
}

