namespace Api.Conhecimento.Rag;

/// <summary>
/// Cálculo da similaridade do cosseno entre dois vetores.
/// </summary>
public static class SimilaridadeCosseno
{
    /// <summary>
    /// Retorna um valor entre -1 e 1: 1 para vetores na mesma direção, 0 para ortogonais
    /// e -1 para opostos. Se algum vetor tiver norma zero, retorna 0.
    /// </summary>
    /// <exception cref="ArgumentException">Os vetores têm tamanhos diferentes ou estão vazios.</exception>
    public static double Calcular(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException($"Os vetores devem ter o mesmo tamanho ({a.Length} e {b.Length}).");
        }

        if (a.IsEmpty)
        {
            throw new ArgumentException("Os vetores não podem estar vazios.");
        }

        double produtoEscalar = 0;
        double normaA = 0;
        double normaB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            produtoEscalar += a[i] * (double)b[i];
            normaA += a[i] * (double)a[i];
            normaB += b[i] * (double)b[i];
        }

        if (normaA == 0 || normaB == 0)
        {
            return 0;
        }

        return produtoEscalar / (Math.Sqrt(normaA) * Math.Sqrt(normaB));
    }
}
