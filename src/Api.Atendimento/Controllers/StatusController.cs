using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

/// <summary>
/// Informações básicas sobre a disponibilidade da API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusController : ControllerBase
{
    /// <summary>
    /// Retorna o nome do serviço e o horário atual (UTC), indicando que a API está no ar.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<StatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<StatusResponse> Get()
        => Ok(new StatusResponse("Api.Atendimento", DateTimeOffset.UtcNow));
}

/// <summary>
/// Resposta do endpoint de status.
/// </summary>
/// <param name="Servico">Nome do serviço.</param>
/// <param name="DataHoraUtc">Horário atual do servidor em UTC.</param>
public record StatusResponse(string Servico, DateTimeOffset DataHoraUtc);
