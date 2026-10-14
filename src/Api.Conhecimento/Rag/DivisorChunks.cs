using System.Text;
using Api.Conhecimento.Configuracoes;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Rag;

public interface IDivisorChunks
{
    IReadOnlyList<Chunk> Dividir(Documento documento);
}

/// <summary>
/// Divide um documento Markdown em chunks. Agrupa parágrafos inteiros até o tamanho máximo,
/// começa um chunk novo a cada título e repete o título da seção no início de cada chunk
/// da mesma seção. Parágrafos maiores que o limite são cortados em pedaços do tamanho máximo.
/// </summary>
public sealed class DivisorChunks(IOptions<RagOptions> options) : IDivisorChunks
{
    public IReadOnlyList<Chunk> Dividir(Documento documento)
    {
        var tamanhoMaximo = options.Value.TamanhoMaximoChunk;
        var textos = new List<string>();
        var titulo = string.Empty;
        var atual = new StringBuilder();
        var temConteudo = false;

        void Fechar()
        {
            if (temConteudo)
            {
                textos.Add(atual.ToString().Trim());
            }

            atual.Clear();
            temConteudo = false;
            if (titulo.Length > 0)
            {
                atual.Append(titulo).Append("\n\n");
            }
        }

        foreach (var paragrafo in SepararParagrafos(documento.Conteudo))
        {
            if (paragrafo.StartsWith('#'))
            {
                titulo = string.Empty;
                Fechar();
                titulo = paragrafo;
                atual.Append(titulo).Append("\n\n");
                continue;
            }

            foreach (var parte in Cortar(paragrafo, tamanhoMaximo))
            {
                if (temConteudo && atual.Length + parte.Length > tamanhoMaximo)
                {
                    Fechar();
                }

                atual.Append(parte).Append("\n\n");
                temConteudo = true;
            }
        }

        Fechar();

        return textos.Select((texto, indice) => new Chunk(documento.Nome, indice, texto)).ToList();
    }

    private static string[] SepararParagrafos(string conteudo)
        => conteudo
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<string> Cortar(string texto, int tamanhoMaximo)
    {
        var inicio = 0;

        while (texto.Length - inicio > tamanhoMaximo)
        {
            // Corta no último espaço dentro do limite para não partir palavras;
            // sem espaço no trecho, corta exatamente no limite.
            var espaco = texto.LastIndexOfAny([' ', '\n', '\t'], inicio + tamanhoMaximo, tamanhoMaximo + 1);
            var fim = espaco > inicio ? espaco : inicio + tamanhoMaximo;

            yield return texto[inicio..fim].TrimEnd();

            inicio = fim;
            while (inicio < texto.Length && char.IsWhiteSpace(texto[inicio]))
            {
                inicio++;
            }
        }

        if (inicio < texto.Length)
        {
            yield return texto[inicio..];
        }
    }
}
