using System.Net;
using Worker.Suporte.Email;
using Worker.Suporte.Mensagens;

namespace Worker.Suporte.Tests.Email;

public class ModeloEmailSuporteTests
{
    private static readonly DateTime Agora = new(2026, 10, 13, 19, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Montar_AssuntoComONomeDoUsuario()
    {
        var email = ModeloEmailSuporte.Montar(new MensagemAtendimento(7, "Maria", "Resumo"), Agora);

        Assert.Equal("Novo atendimento solicitado por: Maria", email.Assunto);
    }

    [Fact]
    public void Montar_HtmlComUsuarioProtocoloDataEDescricao()
    {
        var email = ModeloEmailSuporte.Montar(
            new MensagemAtendimento(7, "Maria", "Pediu atendimento humano.\nDúvida sobre a CBS."), Agora);

        Assert.StartsWith("<!DOCTYPE html>", email.Html);
        Assert.Contains("Maria", email.Html);
        Assert.Contains("#7", email.Html);
        Assert.Contains("13/10/2026 19:30 UTC", email.Html);
        // HtmlEncode converte acentos em entidades (ú -> &#250;); o texto decodificado é o original.
        Assert.Contains("Pediu atendimento humano.<br>Dúvida sobre a CBS.", WebUtility.HtmlDecode(email.Html));
        Assert.Contains("Dúvida sobre a CBS.", email.Texto);
    }

    [Fact]
    public void Montar_ConteudoComHtml_EEscapado()
    {
        var email = ModeloEmailSuporte.Montar(
            new MensagemAtendimento(7, "<b>Maria</b>", "<script>alert(1)</script>"), Agora);

        Assert.DoesNotContain("<script>", email.Html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", email.Html);
        Assert.Contains("&lt;b&gt;Maria&lt;/b&gt;", email.Html);
    }

    [Fact]
    public void Montar_UsuarioComQuebraDeLinha_NaoQuebraOAssunto()
    {
        var email = ModeloEmailSuporte.Montar(new MensagemAtendimento(7, "Maria\r\nBcc: x@y.com", "Resumo"), Agora);

        Assert.DoesNotContain("\n", email.Assunto);
        Assert.DoesNotContain("\r", email.Assunto);
    }
}
