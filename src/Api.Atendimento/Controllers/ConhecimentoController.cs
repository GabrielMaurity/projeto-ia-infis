using Api.Atendimento.Modelos;
using Api.Atendimento.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

/// <summary>
/// Endpoints para validar a comunicação MCP com a Api.Conhecimento.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConhecimentoController(IConhecimentoMcpClient conhecimentoMcpClient) : ControllerBase
{
    /// <summary>
    /// Lista as ferramentas descobertas no MCP Server da Api.Conhecimento.
    /// </summary>
    [HttpGet("ferramentas")]
    [ProducesResponseType<IReadOnlyList<FerramentaMcp>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<FerramentaMcp>>> ListarFerramentas(CancellationToken cancellationToken)
    {
        var ferramentas = await conhecimentoMcpClient.ListarFerramentasAsync(cancellationToken);
        return Ok(ferramentas);
    }

    /// <summary>
    /// Chama a tool BuscarConhecimento via MCP.
    /// </summary>
    [HttpPost("buscar")]
    [ProducesResponseType<BuscarConhecimentoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BuscarConhecimentoResponse>> Buscar(
        BuscarConhecimentoRequest request,
        CancellationToken cancellationToken)
    {
        var conteudo = await conhecimentoMcpClient.BuscarConhecimentoAsync(request.Pergunta, cancellationToken);
        return Ok(new BuscarConhecimentoResponse(conteudo));
    }
}
