using System.Net;
using System.Net.Mail;

public class EmailService
{
    private readonly Config _config;

    public EmailService(Config config)
    {
        _config = config;
    }

    public async Task EnviarAlertaAsync(string tipo, string ativo, double preco, double referencia)
    {
        var assunto = $"ALERTA DE {tipo}: {ativo}";
        var corpo = $"Preço atual: R$ {preco:0.00} - {(tipo == "VENDA" ? "acima de" : "abaixo de")} R$ {referencia:0.00}";

        using var smtp = new SmtpClient(_config.Smtp.Host, _config.Smtp.Port)
        {
            Credentials = new NetworkCredential(_config.Smtp.Username, _config.Smtp.Password),
            EnableSsl = true
        };

        var mail = new MailMessage(_config.Smtp.Username, _config.EmailDestino, assunto, corpo);
        await smtp.SendMailAsync(mail);
    }
}

