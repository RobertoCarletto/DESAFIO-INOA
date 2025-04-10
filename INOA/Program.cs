using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: stock-quote-alert.exe STOCK_SYMBOL SELL_THRESHOLD BUY_THRESHOLD");
            return;
        }

        string stockSymbol = args[0];
        double sellThreshold = double.Parse(args[1]);
        double buyThreshold = double.Parse(args[2]);

        var configJson = await File.ReadAllTextAsync("config.json");
        var config = JsonSerializer.Deserialize<Config>(configJson);
        var emailService = new EmailService(config);
        var stockService = new StockService();

        bool hasSentBuyAlert = false;
        bool hasSentSellAlert = false;

        while (true)
        {
            try
            {
                double currentPrice = await stockService.GetCurrentPriceAsync(stockSymbol);
                Console.WriteLine($"{DateTime.Now:T} | {stockSymbol}: R$ {currentPrice}");

                if (currentPrice > sellThreshold && !hasSentSellAlert)
                {
                    await emailService.SendAlertAsync("SELL", stockSymbol, currentPrice, sellThreshold);
                    hasSentSellAlert = true;
                    hasSentBuyAlert = false;
                }
                else if (currentPrice < buyThreshold && !hasSentBuyAlert)
                {
                    await emailService.SendAlertAsync("BUY", stockSymbol, currentPrice, buyThreshold);
                    hasSentBuyAlert = true;
                    hasSentSellAlert = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}
