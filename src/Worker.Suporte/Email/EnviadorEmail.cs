using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Worker.Suporte.Configuracoes;

namespace Worker.Suporte.Email;

public interface IEnviadorEmail
{
    Task EnviarAsync(EmailSuporte email, CancellationToken cancellationToken);
}

/// <summary>
/// Envia por SMTP com o MailKit. O Mailhog aceita conexões sem TLS e sem autenticação.
/// </summary>
public sealed class EnviadorEmailSmtp(IOptions<EmailOptions> options) : IEnviadorEmail
{
    public async Task EnviarAsync(EmailSuporte email, CancellationToken cancellationToken)
    {
        var config = options.Value;

        var mensagem = new MimeMessage();
        mensagem.From.Add(MailboxAddress.Parse(config.Remetente));
        mensagem.To.Add(MailboxAddress.Parse(config.Destinatario));
        mensagem.Subject = email.Assunto;
        mensagem.Body = new BodyBuilder { HtmlBody = email.Html, TextBody = email.Texto }.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(config.Host, config.Porta, SecureSocketOptions.None, cancellationToken);
        await smtp.SendAsync(mensagem, cancellationToken);
        await smtp.DisconnectAsync(quit: true, cancellationToken);
    }
}
