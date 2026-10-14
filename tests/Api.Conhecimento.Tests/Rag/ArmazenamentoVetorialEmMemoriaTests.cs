using Api.Conhecimento.Rag;

namespace Api.Conhecimento.Tests.Rag;

public class ArmazenamentoVetorialEmMemoriaTests
{
    [Fact]
    public void BuscarMaisSimilares_RetornaOsTresMaisSemelhantesEmOrdemDecrescente()
    {
        var armazenamento = new ArmazenamentoVetorialEmMemoria();
        armazenamento.Substituir(
        [
            Item("oposto", [-1f, 0f]),
            Item("igual", [1f, 0f]),
            Item("ortogonal", [0f, 1f]),
            Item("proximo", [0.9f, 0.1f]),
            Item("medio", [0.5f, 0.5f]),
        ]);

        var resultado = armazenamento.BuscarMaisSimilares([1f, 0f], 3);

        Assert.Equal(["igual", "proximo", "medio"], resultado.Select(r => r.Chunk.Texto));
        Assert.True(resultado[0].Similaridade >= resultado[1].Similaridade);
        Assert.True(resultado[1].Similaridade >= resultado[2].Similaridade);
    }

    [Fact]
    public void BuscarMaisSimilares_ComArmazenamentoVazio_RetornaListaVazia()
    {
        var armazenamento = new ArmazenamentoVetorialEmMemoria();

        Assert.Empty(armazenamento.BuscarMaisSimilares([1f, 0f], 3));
    }

    private static ChunkIndexado Item(string texto, float[] embedding)
        => new(new Chunk("teste.md", 0, texto), embedding);
}
