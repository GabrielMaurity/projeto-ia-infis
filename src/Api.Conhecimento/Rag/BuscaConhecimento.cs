using Api.Conhecimento.Configuracoes;
using Api.Conhecimento.Ingestao;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Rag;

public interface IBuscaConhecimento
{
    Task<IReadOnlyList<TrechoEncontrado>> BuscarAsync(string pergunta, CancellationToken cancellationToken);
}

/// <summary>
/// Gera o embedding da pergunta e devolve os chunks mais semelhantes gravados no banco RAG
/// (PostgreSQL + pgvector), apenas de documentos do agente que ainda estão na validade.
/// </summary>
public sealed class BuscaConhecimento(
    IGeradorEmbeddings geradorEmbeddings,
    IRepositorioChunks repositorio,
    IOptions<RagOptions> ragOptions,
    IOptions<IngestaoOptions> ingestaoOptions,
    TimeProvider timeProvider) : IBuscaConhecimento
{
    public async Task<IReadOnlyList<TrechoEncontrado>> BuscarAsync(string pergunta, CancellationToken cancellationToken)
    {
        var embeddings = await geradorEmbeddings.GerarAsync([pergunta], cancellationToken);
        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);

        return await repositorio.BuscarMaisSimilaresAsync(
            ingestaoOptions.Value.IdAgente,
            embeddings[0],
            ragOptions.Value.QuantidadeResultados,
            hoje,
            cancellationToken);
    }
}
