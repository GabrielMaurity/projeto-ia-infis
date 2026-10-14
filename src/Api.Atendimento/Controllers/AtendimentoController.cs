using Api.Atendimento.Modelos;
using Api.Atendimento.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

/// <summary>
/// Atendimento de dúvidas tributárias e contábeis com base no conhecimento corporativo.
/// </summary>
[ApiController]
[Route("api/v1/atendimentos")]
[Produces("application/json")]
public class AtendimentoController(IAtendimentoService atendimentoService) : ControllerBase
{
    /// <summary>
    /// Registra um novo atendimento e responde à pergunta usando o contexto obtido
    /// da Api.Conhecimento via MCP (busca vetorial no banco RAG) e a OpenAI.
    /// </summary>
    /// <response code="200">Resposta estruturada do assistente.</response>
    /// <response code="400">Nome do usuário ou pergunta inválidos.</response>
    /// <response code="503">Api.Conhecimento indisponível.</response>
    [HttpPost]
    [ProducesResponseType<RespostaAtendimento>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RespostaAtendimento>> Criar(
        AtendimentoRequest request,
        CancellationToken cancellationToken)
    {
        var resposta = await atendimentoService.ResponderAsync(
            request.NomeUsuario.Trim(), request.Pergunta.Trim(), cancellationToken);

        return Ok(resposta);
    }
}
