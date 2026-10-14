namespace Api.Conhecimento.Rag;

public interface IArmazenamentoVetorial
{
    void Substituir(IReadOnlyList<ChunkIndexado> itens);

    IReadOnlyList<TrechoEncontrado> BuscarMaisSimilares(float[] consulta, int quantidade);
}

/// <summary>
/// Armazena os embeddings apenas em memória. O conteúdo se perde quando a API é reiniciada
/// e é recriado pela indexação na subida.
/// </summary>
public sealed class ArmazenamentoVetorialEmMemoria : IArmazenamentoVetorial
{
    private volatile ChunkIndexado[] _itens = [];

    public void Substituir(IReadOnlyList<ChunkIndexado> itens) => _itens = itens.ToArray();

    public IReadOnlyList<TrechoEncontrado> BuscarMaisSimilares(float[] consulta, int quantidade)
        => _itens
            .Select(item => new TrechoEncontrado(item.Chunk, SimilaridadeCosseno.Calcular(consulta, item.Embedding)))
            .OrderByDescending(t => t.Similaridade)
            .Take(quantidade)
            .ToList();
}
