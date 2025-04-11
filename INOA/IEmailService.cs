public interface IEmailService
{
    Task SendAlertAsync(string alertType, string stockSymbol, double currentPrice, double threshold);
}