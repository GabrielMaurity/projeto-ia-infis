using System.Text.Json;
using Api.Atendimento.Servicos.Llm;

namespace Api.Atendimento.Tests.Servicos;

public class SaidaEstruturadaTests
{
    [Fact]
    public void Ler_JsonValido_RetornaOsCampos()
    {
        var saida = SaidaEstruturada.Ler(
            """{"resposta":"A CBS é 0,9%.","categoria":"Tributária","fontes":["reforma.pdf"]}""", "O Gemini");

        Assert.Equal("A CBS é 0,9%.", saida.Resposta);
        Assert.Equal("Tributária", saida.Categoria);
        Assert.Equal(["reforma.pdf"], saida.Fontes);
        Assert.False(saida.EncaminharSuporteHumano);
        Assert.Equal(string.Empty, saida.ResumoEncaminhamento);
    }

    [Fact]
    public void Ler_ComEncaminhamento_RetornaFlagEResumo()
    {
        var saida = SaidaEstruturada.Ler(
            """{"resposta":"Vou encaminhar.","categoria":"Tributária","fontes":[],"encaminharSuporteHumano":true,"resumoEncaminhamento":"  Usuário pediu um atendente.  "}""",
            "A OpenAI");

        Assert.True(saida.EncaminharSuporteHumano);
        Assert.Equal("Usuário pediu um atendente.", saida.ResumoEncaminhamento);
    }

    [Fact]
    public void JsonSchema_ExigeOFlagEOResumoDeEncaminhamento()
    {
        using var schema = JsonDocument.Parse(SaidaEstruturada.JsonSchema);

        var obrigatorios = schema.RootElement.GetProperty("required").EnumerateArray().Select(c => c.GetString());

        Assert.Contains("encaminharSuporteHumano", obrigatorios);
        Assert.Contains("resumoEncaminhamento", obrigatorios);
    }

    [Fact]
    public void Ler_SemFontes_RetornaListaVazia()
    {
        var saida = SaidaEstruturada.Ler("""{"resposta":"Não sei.","categoria":"Sem informação"}""", "O Gemini");

        Assert.Empty(saida.Fontes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("""{"resposta":"","categoria":"Tributária","fontes":[]}""")]
    [InlineData("""{"categoria":"Tributária","fontes":[]}""")]
    public void Ler_RespostaVaziaOuIncompleta_LancaInvalidOperation(string? json)
    {
        var erro = Assert.Throws<InvalidOperationException>(() => SaidaEstruturada.Ler(json, "O Gemini"));

        Assert.Contains("Gemini", erro.Message);
    }

    [Fact]
    public void JsonSchema_ListaAsMesmasCategoriasDoPrompt()
    {
        using var schema = JsonDocument.Parse(SaidaEstruturada.JsonSchema);

        var categorias = schema.RootElement.GetProperty("properties").GetProperty("categoria").GetProperty("enum")
            .EnumerateArray().Select(c => c.GetString());

        Assert.Equal(PromptAtendimento.Categorias, categorias);
    }

    [Fact]
    public void CalcularTokensSaida_DoGemini_SomaRespostaERaciocinio()
    {
        Assert.Equal(350, GeminiLlmClient.CalcularTokensSaida(200, 150));
        Assert.Equal(200, GeminiLlmClient.CalcularTokensSaida(200, null));
        Assert.Equal(0, GeminiLlmClient.CalcularTokensSaida(null, null));
    }
}
