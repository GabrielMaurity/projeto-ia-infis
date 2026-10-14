using System.Text;
using Worker.Suporte.Mensagens;

namespace Worker.Suporte.Tests.Mensagens;

public class MensagemAtendimentoTests
{
    [Fact]
    public void Ler_JsonPublicadoPelaApiAtendimento_RetornaAMensagem()
    {
        var mensagem = MensagemAtendimento.Ler(
            Encoding.UTF8.GetBytes("""{"id":7,"nomeUsuario":"Maria","descricao":"Pediu atendimento humano."}"""));

        Assert.Equal(new MensagemAtendimento(7, "Maria", "Pediu atendimento humano."), mensagem);
    }

    [Theory]
    [InlineData("não é json")]
    [InlineData("""{"id":7,"descricao":"Sem usuário"}""")]
    [InlineData("""{"id":7,"nomeUsuario":"Maria","descricao":""}""")]
    [InlineData("null")]
    public void Ler_JsonInvalidoOuIncompleto_RetornaNull(string json)
    {
        Assert.Null(MensagemAtendimento.Ler(Encoding.UTF8.GetBytes(json)));
    }
}
