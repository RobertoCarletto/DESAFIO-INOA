using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Globalization;

class Program
{
    static async Task Main(string[] args)
    {
        string configPath = "config.json";
        string assetsPath = "assets.args";

        IStockService stockService = new StockService();

        if (args.Length == 0)
        {
            Console.WriteLine("Welcome to Stock Quote Alert!");
            ShowHelp();
        }

        if (args.Length == 1)
        {
            switch (args[0])
            {
                case "--list-assets":
                    await stockService.ListAvailableAssetsAsync();
                    return;
                
                case "--show-log":
                    string logPath = Path.Combine("logs", "alerts.log");
                    if (!File.Exists(logPath))
                    {
                        Console.WriteLine("No logs available.");
                    }
                    else
                    {
                        Console.WriteLine("--- Log Content ---");
                        Console.WriteLine(File.ReadAllText(logPath));
                        Console.WriteLine("--------------------");
                    }
                    return;

                case "--edit-email":
                    if (!File.Exists(configPath))
                    {
                        Console.WriteLine("Configuration file not found.");
                        return;
                    }
                    var configJson = await File.ReadAllTextAsync(configPath);
                    var configEmail = JsonSerializer.Deserialize<Config>(configJson);

                    if (configEmail == null || configEmail.Smtp == null)
                    {
                        Console.WriteLine("Invalid configuration file.");
                        return;
                    }

                    string newEmail;
                    do
                    {
                        Console.Write("Enter new email: ");
                        newEmail = Console.ReadLine()?.Trim() ?? "";
                    } while (!IsValidEmail(newEmail));

                    using (var doc = JsonDocument.Parse(configJson))
                    using (var stream = File.Create(configPath))
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                    {
                        writer.WriteStartObject();
                        foreach (var property in doc.RootElement.EnumerateObject())
                        {
                            if (property.NameEquals("EmailDestination"))
                                writer.WriteString("EmailDestination", newEmail);
                            else
                                property.WriteTo(writer);
                        }
                        writer.WriteEndObject();
                    }
                    Log($"Email destination updated to {newEmail}");
                    Console.WriteLine("Email updated successfully.");
                    return;

                case "--edit-assets":
                    Console.WriteLine("Enter new stock alert data (SYMBOL SELL_THRESHOLD BUY_THRESHOLD ...):");
                    Console.Write("> ");
                    var newAssets = Console.ReadLine() ?? "";
                    File.WriteAllText(assetsPath, newAssets);
                    Log("Stock asset list updated manually via --edit-assets");
                    Console.WriteLine("Assets updated successfully.");
                    return;

                case "--reset-config":
                    if (!File.Exists(configPath))
                    {
                        Console.WriteLine("Configuration file not found.");
                        return;
                    }

                    var originalJson = await File.ReadAllTextAsync(configPath);
                    var originalConfig = JsonSerializer.Deserialize<Config>(originalJson);

                    if (originalConfig == null || originalConfig.Smtp == null)
                    {
                        Console.WriteLine("Invalid configuration file.");
                        return;
                    }

                    originalConfig.EmailDestination = "";
                    originalConfig.CheckIntervalSeconds = 0;
                    string updatedJson = JsonSerializer.Serialize(originalConfig, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(configPath, updatedJson);
                    File.WriteAllText(assetsPath, "");

                    Log("Configuration reset via --reset-config");
                    Console.WriteLine("Configuration reset. Restart the program to reconfigure.");
                    return;

                case "--edit-interval":
                    if (!File.Exists(configPath))
                    {
                        Console.WriteLine("Configuration file not found.");
                        return;
                    }

                    var configJsonInterval = await File.ReadAllTextAsync(configPath);
                    var configEditInterval = JsonSerializer.Deserialize<Config>(configJsonInterval);

                    if (configEditInterval == null || configEditInterval.Smtp == null)
                    {
                        Console.WriteLine("Invalid configuration file.");
                        return;
                    }

                    string newIntervalRaw;
                    int newInterval;
                    do
                    {
                        Console.Write("Enter new check interval in seconds (min 60): ");
                        newIntervalRaw = Console.ReadLine()?.Trim() ?? "";
                        if (!int.TryParse(newIntervalRaw, out newInterval) || newInterval < 60)
                        {
                            Console.Write("Invalid interval, please try again!\n");
                        }
                    } while (!int.TryParse(newIntervalRaw, out newInterval) || newInterval < 60);

                    using (var doc = JsonDocument.Parse(configJsonInterval))
                    using (var stream = File.Create(configPath))
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                    {
                        writer.WriteStartObject();
                        foreach (var property in doc.RootElement.EnumerateObject())
                        {
                            if (property.NameEquals("CheckIntervalSeconds"))
                                writer.WriteNumber("CheckIntervalSeconds", newInterval);
                            else
                                property.WriteTo(writer);
                        }
                        writer.WriteEndObject();
                    }

                    Log($"Check interval updated to {newInterval} seconds");
                    Console.WriteLine("Check interval updated successfully.");
                    return;

                default:
                    Console.WriteLine("Unknown command. Use --help to see available options.");
                    return;
            }
        }

        Config config;

        if (!File.Exists(configPath))
        {
            Console.WriteLine("Configuration file 'config.json' not found.");
            return;
        }

        var rawConfig = await File.ReadAllTextAsync(configPath);
        config = JsonSerializer.Deserialize<Config>(rawConfig);

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

            using var jsonDoc = JsonDocument.Parse(rawConfig);
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
            Log($"Email destination set to {emailInput}");
        }

        if (config.CheckIntervalSeconds < 60)
        {
            string intervalInput;
            int interval;
            do
            {
                Console.Write("Please enter the check interval in seconds (minimum 60): ");
                intervalInput = Console.ReadLine()?.Trim() ?? "";
                if (!int.TryParse(intervalInput, out interval) || interval < 60)
                {
                    Console.Write("Invalid interval, please try again!\n");
                }
            } while (!int.TryParse(intervalInput, out interval) || interval < 60);

            config.CheckIntervalSeconds = interval;

            using var jsonDoc = JsonDocument.Parse(rawConfig);
            using var stream = File.Create(configPath);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

            writer.WriteStartObject();
            foreach (var property in jsonDoc.RootElement.EnumerateObject())
            {
                if (property.NameEquals("CheckIntervalSeconds"))
                {
                    writer.WriteNumber("CheckIntervalSeconds", interval);
                }
                else if (property.NameEquals("EmailDestination"))
                {
                    writer.WriteString("EmailDestination", config.EmailDestination);
                }
                else
                {
                    property.WriteTo(writer);
                }
            }
            writer.WriteEndObject();
            await writer.FlushAsync();
            Log($"Check interval set to {interval} seconds");
        }

        IEmailService emailService = new EmailService(config);

        string[] assetArgs;

        if (args.Length >= 3 && args.Length % 3 == 0)
        {
            assetArgs = args;
        }
        else if (File.Exists(assetsPath))
        {
            var raw = File.ReadAllText(assetsPath).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                Console.WriteLine("No stock data found in 'assets.args'. Please enter stocks in the format:");
                Console.WriteLine("SYMBOL SELL_THRESHOLD BUY_THRESHOLD (multiple sets separated by spaces)");
                Console.Write("Example: PETR4 22.67 22.59 VALE3 65.50 60.00\n> ");
                raw = Console.ReadLine() ?? "";
                File.WriteAllText(assetsPath, raw);
                Log("Stock asset list configured interactively (missing assets.args)");
            }
            assetArgs = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            Console.WriteLine("No stock data found. Please enter stocks in the format:");
            Console.WriteLine("SYMBOL SELL_THRESHOLD BUY_THRESHOLD (multiple sets separated by spaces)");
            Console.Write("Example: PETR4 22.67 22.59 VALE3 65.50 60.00\n> ");
            var input = Console.ReadLine() ?? "";
            File.WriteAllText(assetsPath, input);
            assetArgs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Log("Stock asset list configured interactively (no assets.args found)");
        }

        var monitors = new List<StockAlertMonitor>();

        for (int i = 0; i < assetArgs.Length; i += 3)
        {
            string stockSymbol = assetArgs[i];

            bool parsedSell = double.TryParse(assetArgs[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double sellThreshold);
            bool parsedBuy = double.TryParse(assetArgs[i + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out double buyThreshold);

            if (!parsedSell || !parsedBuy)
            {
                Console.WriteLine($"Invalid thresholds for {stockSymbol}. Skipping...");
                Log($"Invalid input for {stockSymbol}: {assetArgs[i + 1]} / {assetArgs[i + 2]}");
                continue;
            }

            var monitor = new StockAlertMonitor(stockSymbol, sellThreshold, buyThreshold, stockService, emailService);
            monitors.Add(monitor);
            Log($"Monitoring {stockSymbol}: SELL > {sellThreshold}, BUY < {buyThreshold}");
        }


        if (monitors.Count == 0)
        {
            Console.WriteLine("No valid stocks to monitor. Exiting.");
            Log("No valid monitors initialized. Program exited.");
            return;
        }

        Log($"Monitoring started for {monitors.Count} assets.");
        await Task.WhenAll(monitors.Select(m => m.RunAsync(config.CheckIntervalSeconds)));
    }

    static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    static void ShowHelp()
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine("  --edit-email     Edit alert destination email");
        Console.WriteLine("  --edit-assets    Edit monitored stocks and thresholds");
        Console.WriteLine("  --edit-interval  Edit the check interval between stock checks");
        Console.WriteLine("  --reset-config   Clear email,asset and interval configuration");
        Console.WriteLine("  --show-log       View log entries");
        Console.WriteLine("  --list-assets    List available B3 stock symbols from brapi.dev");
        Console.WriteLine("  [symbol sell buy ...]   Run alerts manually");
        Console.WriteLine();
    }

    static void Log(string message)
    {
        var logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
        var logPath = Path.Combine("logs", "alerts.log");
        Directory.CreateDirectory("logs");
        File.AppendAllText(logPath, logLine + Environment.NewLine);
    }
}
