namespace Api.Conhecimento.Rag;

/// <summary>
/// Trecho de um documento usado na busca vetorial.
/// </summary>
/// <param name="Fonte">Nome do arquivo de origem.</param>
/// <param name="Indice">Posição do trecho dentro do documento (começa em 0).</param>
/// <param name="Texto">Conteúdo do trecho.</param>
public record Chunk(string Fonte, int Indice, string Texto);

/// <summary>
/// Chunk com seu embedding.
/// </summary>
public record ChunkIndexado(Chunk Chunk, float[] Embedding);

/// <summary>
/// Chunk encontrado na busca, com a similaridade em relação à pergunta.
/// </summary>
public record TrechoEncontrado(Chunk Chunk, double Similaridade);
