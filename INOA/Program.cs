using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 3 || args.Length % 3 != 0)
        {
            Console.WriteLine("Usage: stock-quote-alert.exe [STOCK_SYMBOL SELL_THRESHOLD BUY_THRESHOLD] ...");
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

        var monitors = new List<StockAlertMonitor>();

        for (int i = 0; i < args.Length; i += 3)
        {
            string stockSymbol = args[i];
            if (!double.TryParse(args[i + 1], out double sellThreshold) ||
                !double.TryParse(args[i + 2], out double buyThreshold))
            {
                Console.WriteLine($"Invalid thresholds for {stockSymbol}. Skipping...");
                continue;
            }

            var monitor = new StockAlertMonitor(stockSymbol, sellThreshold, buyThreshold, stockService, emailService);
            monitors.Add(monitor);
        }

        if (monitors.Count == 0)
        {
            Console.WriteLine("No valid stocks to monitor. Exiting.");
            return;
        }

        await Task.WhenAll(monitors.Select(m => m.RunAsync()));
    }
}
