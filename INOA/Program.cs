using System.Text.Json;
using INOA;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Formato inválido!\nUso: stock-quote-alert.exe ATIVO LIMITE_VENDA LIMITE_COMPRA");
            return;
        }

        string ativo = args[0];
        double limiteVenda = double.Parse(args[1]);
        double limiteCompra = double.Parse(args[2]);

        var configText = await File.ReadAllTextAsync("config.json");
        var config = JsonSerializer.Deserialize<Config>(configText);
        var emailService = new EmailService(config);
        var stockService = new StockService();

        bool alertadoCompra = false;
        bool alertadoVenda = false;

        while (true)
        {
            try
            {
                double precoAtual = await stockService.ObterPrecoAtualAsync(ativo);
                Console.WriteLine($"{DateTime.Now:T} | {ativo}: R$ {precoAtual}");

                if (precoAtual > limiteVenda && !alertadoVenda)
                {
                    await emailService.EnviarAlertaAsync("VENDA", ativo, precoAtual, limiteVenda);
                    alertadoVenda = true;
                    alertadoCompra = false;
                }
                else if (precoAtual < limiteCompra && !alertadoCompra)
                {
                    await emailService.EnviarAlertaAsync("COMPRA", ativo, precoAtual, limiteCompra);
                    alertadoCompra = true;
                    alertadoVenda = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}
