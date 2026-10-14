namespace Api.Conhecimento.Rag;

public interface IIndexadorDocumentos
{
    Task IndexarAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Lê os documentos, divide em chunks, gera os embeddings e guarda no armazenamento vetorial.
/// </summary>
public sealed class IndexadorDocumentos(
    ILeitorDocumentos leitor,
    IDivisorChunks divisor,
    IGeradorEmbeddings geradorEmbeddings,
    IArmazenamentoVetorial armazenamento,
    ILogger<IndexadorDocumentos> logger) : IIndexadorDocumentos
{
    public async Task IndexarAsync(CancellationToken cancellationToken)
    {
        var documentos = await leitor.LerAsync(cancellationToken);
        var chunks = documentos.SelectMany(divisor.Dividir).ToList();

        if (chunks.Count == 0)
        {
            logger.LogWarning("Nenhum chunk gerado; a base de conhecimento ficará vazia.");
            armazenamento.Substituir([]);
            return;
        }

        logger.LogInformation("{Chunks} chunk(s) gerado(s) a partir de {Documentos} documento(s). Gerando embeddings.",
            chunks.Count, documentos.Count);

        var embeddings = await geradorEmbeddings.GerarAsync(chunks.Select(c => c.Texto).ToList(), cancellationToken);

        armazenamento.Substituir(chunks.Zip(embeddings, (chunk, embedding) => new ChunkIndexado(chunk, embedding)).ToList());

        logger.LogInformation("Base de conhecimento indexada em memória com {Chunks} chunk(s).", chunks.Count);
    }
}

/// <summary>
/// Executa a indexação em segundo plano quando a API sobe.
/// Uma falha (por exemplo, chave da OpenAI ausente) é registrada no log e não derruba a API.
/// </summary>
public sealed class IndexacaoDocumentosHostedService(
    IIndexadorDocumentos indexador,
    ILogger<IndexacaoDocumentosHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await indexador.IndexarAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao indexar os documentos da base de conhecimento.");
        }
    }
}
