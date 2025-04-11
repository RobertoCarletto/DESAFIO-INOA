public class StockAlertMonitor
{
    private readonly string _stockSymbol;
    private readonly double _sellThreshold;
    private readonly double _buyThreshold;
    private readonly IStockService _stockService;
    private readonly IEmailService _emailService;

    private bool _hasSentBuyAlert = false;
    private bool _hasSentSellAlert = false;

    public StockAlertMonitor(string stockSymbol, double sellThreshold, double buyThreshold,
                              IStockService stockService, IEmailService emailService)
    {
        _stockSymbol = stockSymbol;
        _sellThreshold = sellThreshold;
        _buyThreshold = buyThreshold;
        _stockService = stockService;
        _emailService = emailService;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            try
            {
                double currentPrice = await _stockService.GetCurrentPriceAsync(_stockSymbol);
                if (currentPrice < 0)
                {
                    Console.WriteLine("Skipping this iteration due to previous error.");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now:T} | {_stockSymbol}: R$ {currentPrice}");

                    if (currentPrice > _sellThreshold && !_hasSentSellAlert)
                    {
                        await _emailService.SendAlertAsync("SELL", _stockSymbol, currentPrice, _sellThreshold);
                        _hasSentSellAlert = true;
                        _hasSentBuyAlert = false;
                    }
                    else if (currentPrice < _buyThreshold && !_hasSentBuyAlert)
                    {
                        await _emailService.SendAlertAsync("BUY", _stockSymbol, currentPrice, _buyThreshold);
                        _hasSentBuyAlert = true;
                        _hasSentSellAlert = false;
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

