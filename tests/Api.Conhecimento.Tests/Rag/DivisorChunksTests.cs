using Api.Conhecimento.Configuracoes;
using Api.Conhecimento.Rag;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Tests.Rag;

public class DivisorChunksTests
{
    [Fact]
    public void Dividir_ParagrafoMaiorQueOLimite_CortaSemPartirPalavras()
    {
        var palavras = Enumerable.Range(1, 120).Select(i => $"palavra{i}").ToArray();
        var divisor = Divisor(tamanhoMaximo: 100);

        var chunks = divisor.Dividir(new Documento("doc.pdf", string.Join(' ', palavras)));

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Texto.Length <= 100, $"Chunk com {c.Texto.Length} caracteres."));
        Assert.Equal(palavras, chunks.SelectMany(c => c.Texto.Split(' ', '\n')).Where(p => p.Length > 0));
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(c => c.Indice));
    }

    [Fact]
    public void Dividir_PalavraMaiorQueOLimite_CortaNoLimite()
    {
        var divisor = Divisor(tamanhoMaximo: 100);

        var chunks = divisor.Dividir(new Documento("doc.pdf", new string('a', 250)));

        Assert.Equal([100, 100, 50], chunks.Select(c => c.Texto.Length));
    }

    private static DivisorChunks Divisor(int tamanhoMaximo)
        => new(Options.Create(new RagOptions { TamanhoMaximoChunk = tamanhoMaximo }));
}
