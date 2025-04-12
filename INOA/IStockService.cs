public interface IStockService
{
    Task<double> GetCurrentPriceAsync(string stockSymbol);
    Task ListAvailableAssetsAsync();
}