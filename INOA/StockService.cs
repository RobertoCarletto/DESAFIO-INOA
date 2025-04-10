using System.Net.Http;
using System.Text.Json;

public class StockService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task<double> GetCurrentPriceAsync(string stockSymbol)
    {
        try
        {
            string apiUrl = $"https://brapi.dev/api/quote/{stockSymbol}?range=1d&interval=1m";
            var jsonResponse = await httpClient.GetStringAsync(apiUrl);
            using var jsonDoc = JsonDocument.Parse(jsonResponse);

            var stockData = jsonDoc.RootElement.GetProperty("results")[0];
            return stockData.GetProperty("regularMarketPrice").GetDouble();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error while fetching stock data: {ex.Message}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to parse stock data: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Data structure error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error while retrieving stock price: {ex.Message}");
        }

        return -1;
    }
}