using System.Net.Http;
using System.Text.Json;

public class StockService : IStockService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task<double> GetCurrentPriceAsync(string stockSymbol)
    {
        try
        {
            string token = "iEdno1GoVXexf3S5WdHK9S";
            string apiUrl = $"https://brapi.dev/api/quote/{stockSymbol}?token={token}&range=1d&interval=1d";

            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ERROR] HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                Console.WriteLine($"[ERROR] Resposta da API: {errorContent}");
                return -1;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
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
    public async Task ListAvailableAssetsAsync()
    {
        Console.WriteLine("Fetching available B3 assets from brapi.dev...");
        try
        {
            var response = await httpClient.GetAsync("https://brapi.dev/api/quote/list");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to fetch list: {response.StatusCode} - {response.ReasonPhrase}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var jsonDoc = JsonDocument.Parse(content);
            var results = jsonDoc.RootElement.GetProperty("stocks");

            foreach (var stock in results.EnumerateArray())
            {
                var name = stock.GetProperty("name").GetString();
                var code = stock.GetProperty("stock").GetString();
                Console.WriteLine($"{code} - {name}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while listing assets: {ex.Message}");
        }
    }
}
