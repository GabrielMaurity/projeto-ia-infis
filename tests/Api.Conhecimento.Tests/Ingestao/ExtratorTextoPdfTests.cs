using Api.Conhecimento.Ingestao;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Api.Conhecimento.Tests.Ingestao;

public class ExtratorTextoPdfTests
{
    [Fact]
    public void Extrair_PdfComDuasPaginas_RetornaTextoDasPaginasSeparadoPorLinhaEmBranco()
    {
        var pdf = CriarPdf("Politica de ferias", "Reembolso de despesas");

        var texto = new ExtratorTextoPdf().Extrair(pdf);

        Assert.Equal("Politica de ferias\n\nReembolso de despesas", texto);
    }

    [Fact]
    public void Extrair_PdfSemTexto_RetornaVazio()
    {
        var pdf = CriarPdf();

        Assert.Equal(string.Empty, new ExtratorTextoPdf().Extrair(pdf));
    }

    [Fact]
    public void Extrair_ConteudoInvalido_LancaExcecao()
    {
        Assert.ThrowsAny<Exception>(() => new ExtratorTextoPdf().Extrair("%PDF-lixo"u8.ToArray()));
    }

    private static byte[] CriarPdf(params string[] paginas)
    {
        var builder = new PdfDocumentBuilder();
        var fonte = builder.AddStandard14Font(Standard14Font.Helvetica);

        if (paginas.Length == 0)
        {
            builder.AddPage(PageSize.A4);
        }

        foreach (var texto in paginas)
        {
            builder.AddPage(PageSize.A4).AddText(texto, 12, new PdfPoint(50, 750), fonte);
        }

        return builder.Build();
    }
}
