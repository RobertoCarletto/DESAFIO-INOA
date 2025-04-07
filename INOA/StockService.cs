using System.Net.Http;
using System.Text.Json;

public class StockService
{
    private static readonly HttpClient _http = new HttpClient();

    public async Task<double> ObterPrecoAtualAsync(string ativo)
    {
        string url = $"https://brapi.dev/api/quote/{ativo}?range=1d&interval=1m";
        var response = await _http.GetStringAsync(url);
        using var doc = JsonDocument.Parse(response);

        var result = doc.RootElement.GetProperty("results")[0];
        return result.GetProperty("regularMarketPrice").GetDouble();
    }
}
