using Api.Atendimento.Servicos.Llm;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Atendimento.Tests.Servicos;

public class LlmComFallbackTests
{
    private readonly LlmFalso _openAI = new("gpt-4.1-mini");
    private readonly LlmFalso _gemini = new("gemini-3.5-flash-lite");

    [Fact]
    public async Task GerarRespostaAsync_OpenAIRespondendo_NaoChamaOGemini()
    {
        var resposta = await Fallback().GerarRespostaAsync("instruções", "mensagem", CancellationToken.None);

        Assert.Equal("gpt-4.1-mini", resposta.Modelo);
        Assert.False(resposta.Fallback);
        Assert.Equal(1, _openAI.Chamadas);
        Assert.Equal(0, _gemini.Chamadas);
    }

    [Fact]
    public async Task GerarRespostaAsync_OpenAIFalhou_UsaOGeminiComAsMesmasMensagensEMarcaFallback()
    {
        _openAI.Erro = new HttpRequestException("OpenAI fora do ar");

        var resposta = await Fallback().GerarRespostaAsync("instruções", "mensagem", CancellationToken.None);

        Assert.Equal("gemini-3.5-flash-lite", resposta.Modelo);
        Assert.True(resposta.Fallback);
        Assert.Equal(("instruções", "mensagem"), _gemini.Recebido);
    }

    [Fact]
    public async Task GerarRespostaAsync_OpenAIEstourouOTimeout_UsaOGemini()
    {
        // Timeout do SDK chega como cancelamento sem que o cliente tenha cancelado a requisição.
        _openAI.Erro = new TaskCanceledException("timeout");

        var resposta = await Fallback().GerarRespostaAsync("instruções", "mensagem", CancellationToken.None);

        Assert.True(resposta.Fallback);
    }

    [Fact]
    public async Task GerarRespostaAsync_OsDoisFalharam_LancaLlmIndisponivelComAsDuasFalhas()
    {
        _openAI.Erro = new HttpRequestException("OpenAI fora do ar");
        _gemini.Erro = new InvalidOperationException("Gemini:ApiKey não configurada");

        var erro = await Assert.ThrowsAsync<LlmIndisponivelException>(
            () => Fallback().GerarRespostaAsync("instruções", "mensagem", CancellationToken.None));

        var falhas = Assert.IsType<AggregateException>(erro.InnerException);
        Assert.Equal([_openAI.Erro, _gemini.Erro], falhas.InnerExceptions);
    }

    [Fact]
    public async Task GerarRespostaAsync_CancelamentoPedidoPeloCliente_NaoAcionaOGemini()
    {
        using var cancelamento = new CancellationTokenSource();
        await cancelamento.CancelAsync();
        _openAI.Erro = new OperationCanceledException(cancelamento.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Fallback().GerarRespostaAsync("instruções", "mensagem", cancelamento.Token));

        Assert.Equal(0, _gemini.Chamadas);
    }

    private LlmComFallback Fallback() => new(_openAI, _gemini, NullLogger<LlmComFallback>.Instance);

    private sealed class LlmFalso(string modelo) : ILlmClient
    {
        public Exception? Erro { get; set; }

        public int Chamadas { get; private set; }

        public (string Instrucoes, string Mensagem)? Recebido { get; private set; }

        public Task<RespostaLlm> GerarRespostaAsync(string instrucoes, string mensagem, CancellationToken cancellationToken)
        {
            Chamadas++;
            Recebido = (instrucoes, mensagem);

            return Erro is null
                ? Task.FromResult(new RespostaLlm("Resposta", "Tributária", [], modelo, 10, 5, new CustoChamada(0.1m, 0.2m)))
                : throw Erro;
        }
    }
}
