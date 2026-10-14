using Api.Conhecimento.Configuracoes;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace Api.Conhecimento.Rag;

public interface IGeradorEmbeddings
{
    Task<IReadOnlyList<float[]>> GerarAsync(IReadOnlyList<string> textos, CancellationToken cancellationToken);
}

/// <summary>
/// Gera embeddings com o SDK oficial da OpenAI.
/// </summary>
public sealed class OpenAIGeradorEmbeddings : IGeradorEmbeddings
{
    private const int TamanhoLote = 100;

    private readonly Lazy<EmbeddingClient> _client;

    public OpenAIGeradorEmbeddings(IOptions<OpenAIOptions> options)
    {
        // Criado sob demanda para a API subir mesmo sem a chave configurada;
        // o erro aparece na primeira geração de embeddings.
        _client = new Lazy<EmbeddingClient>(() =>
        {
            var config = options.Value;
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI:ApiKey não configurada. Use user-secrets ou a variável de ambiente OpenAI__ApiKey.");
            }

            return new EmbeddingClient(config.ModeloEmbedding, config.ApiKey);
        });
    }

    public async Task<IReadOnlyList<float[]>> GerarAsync(IReadOnlyList<string> textos, CancellationToken cancellationToken)
    {
        var embeddings = new List<float[]>(textos.Count);

        foreach (var lote in textos.Chunk(TamanhoLote))
        {
            var resultado = await _client.Value.GenerateEmbeddingsAsync(lote, cancellationToken: cancellationToken);
            embeddings.AddRange(resultado.Value.OrderBy(e => e.Index).Select(e => e.ToFloats().ToArray()));
        }

        return embeddings;
    }
}
