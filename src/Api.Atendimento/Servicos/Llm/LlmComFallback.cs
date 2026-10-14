namespace Api.Atendimento.Servicos.Llm;

/// <summary>
/// Nenhum modelo de linguagem conseguiu responder (principal e contingência falharam).
/// </summary>
public sealed class LlmIndisponivelException(string message, Exception innerException)
    : Exception(message, innerException);

/// <summary>
/// Chama sempre o modelo principal (OpenAI) primeiro e, somente quando ele falhar, o modelo de
/// contingência (Gemini), marcando a resposta com <see cref="RespostaLlm.Fallback"/>.
/// O cancelamento pedido pelo cliente não é falha e não aciona a contingência.
/// </summary>
public sealed class LlmComFallback(
    ILlmClient principal,
    ILlmClient contingencia,
    ILogger<LlmComFallback> logger) : ILlmClient
{
    public async Task<RespostaLlm> GerarRespostaAsync(string instrucoes, string mensagem, CancellationToken cancellationToken)
    {
        Exception falhaPrincipal;
        try
        {
            return await principal.GerarRespostaAsync(instrucoes, mensagem, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            falhaPrincipal = ex;
            logger.LogWarning(ex, "O modelo principal (OpenAI) falhou; acionando o modelo de contingência (Gemini).");
        }

        try
        {
            var resposta = await contingencia.GerarRespostaAsync(instrucoes, mensagem, cancellationToken);
            return resposta with { Fallback = true };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "O modelo de contingência (Gemini) também falhou.");
            throw new LlmIndisponivelException(
                "Os modelos de linguagem principal e de contingência estão indisponíveis.",
                new AggregateException(falhaPrincipal, ex));
        }
    }
}
