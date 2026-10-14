using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Erros;

/// <summary>
/// Converte falhas de comunicação com a Api.Conhecimento em resposta 503 (ProblemDetails).
/// </summary>
public sealed class ConhecimentoIndisponivelExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ConhecimentoIndisponivelExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not HttpRequestException)
        {
            return false;
        }

        logger.LogWarning(exception, "Falha ao comunicar com o MCP Server da Api.Conhecimento.");

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Api.Conhecimento indisponível",
                Detail = "Não foi possível comunicar com o MCP Server da Api.Conhecimento."
            }
        });
    }
}
