using Api.Atendimento.Servicos.Llm;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Erros;

/// <summary>
/// Converte a falha dos dois modelos de linguagem (OpenAI e Gemini) em resposta 503 (ProblemDetails).
/// </summary>
public sealed class LlmIndisponivelExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not LlmIndisponivelException)
        {
            return false;
        }

        // O detalhe das falhas já foi registrado no log pelo LlmComFallback.
        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Serviço de IA indisponível",
                Detail = "Não foi possível gerar a resposta: os modelos de linguagem estão indisponíveis. Tente novamente mais tarde."
            }
        });
    }
}
