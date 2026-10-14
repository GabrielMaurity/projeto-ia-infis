using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Api.Conhecimento.Ingestao;

public interface IExtratorTextoPdf
{
    string Extrair(byte[] pdf);
}

/// <summary>
/// Extrai o texto de um PDF com o PdfPig, na ordem de leitura de cada página.
/// As páginas são separadas por linha em branco, que o divisor de chunks trata como parágrafo.
/// PDFs escaneados (só imagem) não têm texto e retornam string vazia.
/// </summary>
public sealed class ExtratorTextoPdf : IExtratorTextoPdf
{
    public string Extrair(byte[] pdf)
    {
        using var documento = PdfDocument.Open(pdf);

        var paginas = documento.GetPages()
            .Select(pagina => ContentOrderTextExtractor.GetText(pagina).Trim())
            .Where(texto => texto.Length > 0);

        // O PostgreSQL não aceita o caractere nulo em colunas TEXT.
        return string.Join("\n\n", paginas).Replace("\0", string.Empty);
    }
}
