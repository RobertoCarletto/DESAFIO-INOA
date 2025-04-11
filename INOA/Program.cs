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

        IEmailService emailService = new EmailService(config);
        IStockService stockService = new StockService();
        var monitor = new StockAlertMonitor(stockSymbol, sellThreshold, buyThreshold, stockService, emailService);
        await monitor.RunAsync();
    }
}
