using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        Config config;
        string configPath = "config.json";

        if (!File.Exists(configPath))
        {
            Console.WriteLine("Configuration file 'config.json' not found.");
            return;
        }

        var configJson = await File.ReadAllTextAsync(configPath);
        config = JsonSerializer.Deserialize<Config>(configJson);

        if (config == null || config.Smtp == null)
        {
            Console.WriteLine("Invalid configuration file.");
            return;
        }

        if (string.IsNullOrWhiteSpace(config.EmailDestination) || !IsValidEmail(config.EmailDestination))
        {
            string emailInput;
            do
            {
                Console.Write("No valid email found. Please enter a valid email to receive alerts: ");
                emailInput = Console.ReadLine()?.Trim() ?? "";
            } while (!IsValidEmail(emailInput));

            using var jsonDoc = JsonDocument.Parse(configJson);
            using var stream = File.Create(configPath);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();

            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                if (property.NameEquals("EmailDestination"))
                {
                    writer.WriteString("EmailDestination", emailInput);
                    config.EmailDestination = emailInput;
                }
                else
                {
                    property.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
            await writer.FlushAsync();
        }

        IEmailService emailService = new EmailService(config);
        IStockService stockService = new StockService();

        string[] assetArgs;

        if (args.Length >= 3 && args.Length % 3 == 0)
        {
            assetArgs = args;
        }
        else if (File.Exists("assets.args"))
        {
            var raw = File.ReadAllText("assets.args").Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                Console.WriteLine("No stock data found in 'assets.args'. Please enter stocks in the format:");
                Console.WriteLine("SYMBOL SELL_THRESHOLD BUY_THRESHOLD (multiple sets separated by spaces)");
                Console.Write("Example: PETR4 22.67 22.59 VALE3 65.50 60.00\n> ");
                raw = Console.ReadLine() ?? "";
                File.WriteAllText("assets.args", raw);
            }
            assetArgs = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            Console.WriteLine("No stock data found. Please enter stocks in the format:");
            Console.WriteLine("SYMBOL SELL_THRESHOLD BUY_THRESHOLD (multiple sets separated by spaces)");
            Console.Write("Example: PETR4 22.67 22.59 VALE3 65.50 60.00\n> ");
            var input = Console.ReadLine() ?? "";
            File.WriteAllText("assets.args", input);
            assetArgs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        var monitors = new List<StockAlertMonitor>();

        for (int i = 0; i < assetArgs.Length; i += 3)
        {
            string stockSymbol = assetArgs[i];
            if (!double.TryParse(assetArgs[i + 1], out double sellThreshold) ||
                !double.TryParse(assetArgs[i + 2], out double buyThreshold))
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

        await Task.WhenAll(monitors.Select(m => m.RunAsync(config.CheckIntervalSeconds)));
    }

    static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}