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
        double sellThreshold;
        double buyThreshold;

        try
        {
            sellThreshold = double.Parse(args[1]);
            buyThreshold = double.Parse(args[2]);
        }
        catch (FormatException)
        {
            Console.WriteLine("Thresholds must be valid decimal numbers.");
            return;
        }

        Config config;
        try
        {
            var configJson = await File.ReadAllTextAsync("config.json");
            config = JsonSerializer.Deserialize<Config>(configJson);

            if (config == null || config.Smtp == null)
            {
                Console.WriteLine("Invalid configuration file.");
                return;
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("Configuration file 'config.json' not found.");
            return;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing configuration file: {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected configuration error: {ex.Message}");
            return;
        }

        var emailService = new EmailService(config);
        var stockService = new StockService();

        bool hasSentBuyAlert = false;
        bool hasSentSellAlert = false;

        while (true)
        {
            try
            {
                double currentPrice = await stockService.GetCurrentPriceAsync(stockSymbol);
                if (currentPrice < 0)
                {
                    Console.WriteLine("Skipping this iteration due to previous error.");
                }
                else
                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected runtime error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}
