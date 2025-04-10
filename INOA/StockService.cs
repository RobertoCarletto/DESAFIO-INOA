using System.Net.Http;
using System.Text.Json;

public class StockService
{
    private static readonly HttpClient httpClient = new HttpClient();

    public async Task<double> GetCurrentPriceAsync(string stockSymbol)
    {
        string apiUrl = $"https://brapi.dev/api/quote/{stockSymbol}?range=1d&interval=1m";
        var jsonResponse = await httpClient.GetStringAsync(apiUrl);
        using var jsonDoc = JsonDocument.Parse(jsonResponse);

        var stockData = jsonDoc.RootElement.GetProperty("results")[0];
        return stockData.GetProperty("regularMarketPrice").GetDouble();
    }
}
