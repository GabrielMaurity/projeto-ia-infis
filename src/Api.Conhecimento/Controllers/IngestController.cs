using Api.Conhecimento.Ingestao;
using Api.Conhecimento.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Conhecimento.Controllers;

/// <summary>
/// Cadastro de documentos na base de conhecimento (RAG).
/// </summary>
[ApiController]
[Route("api/v1/ingest")]
[Produces("application/json")]
public class IngestController(IIngestaoDocumentos ingestao) : ControllerBase
{
    // Limite da requisição inteira; o limite do PDF (2 MB) é validado na ingestão
    // para devolver uma mensagem clara em vez de um 413 sem corpo.
    private const long TamanhoMaximoRequisicao = 10 * 1024 * 1024;

    /// <summary>
    /// Recebe um PDF, extrai o texto, divide em chunks, gera os embeddings e grava no banco RAG.
    /// </summary>
    /// <response code="201">Conhecimento cadastrado com sucesso.</response>
    /// <response code="400">Dados inválidos, arquivo que não é PDF ou maior que 2 MB.</response>
    /// <response code="422">PDF ilegível ou sem texto extraível.</response>
    /// <response code="502">Falha ao gerar os embeddings na OpenAI.</response>
    /// <response code="503">Falha ao gravar no banco RAG.</response>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(TamanhoMaximoRequisicao)]
    [RequestFormLimits(MultipartBodyLengthLimit = TamanhoMaximoRequisicao)]
    [ProducesResponseType<RespostaApi>(StatusCodes.Status201Created)]
    [ProducesResponseType<RespostaApi>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<RespostaApi>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<RespostaApi>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<RespostaApi>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RespostaApi>> Post([FromForm] IngestRequest request, CancellationToken cancellationToken)
    {
        await using var conteudo = request.Arquivo!.OpenReadStream();

        var resultado = await ingestao.IngerirAsync(
            new SolicitacaoIngestao(
                request.NomeUsuario!.Trim(),
                request.CodigoDocumento!.Trim(),
                request.Validade!.Value,
                Path.GetFileName(request.Arquivo.FileName),
                conteudo),
            cancellationToken);

        return StatusCode(resultado.Status, new RespostaApi(resultado.Status, resultado.Mensagem));
    }
}
