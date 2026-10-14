using System.Text;
using Api.Conhecimento.Ingestao;

namespace Api.Conhecimento.Tests.Ingestao;

public class ValidadorArquivoPdfTests
{
    private const long DoisMb = 2 * 1024 * 1024;

    [Fact]
    public void Validar_PdfDentroDoLimite_RetornaNull()
    {
        Assert.Null(ValidadorArquivoPdf.Validar("manual.PDF", Pdf(100), DoisMb));
    }

    [Fact]
    public void Validar_PdfComExatamente2Mb_RetornaNull()
    {
        Assert.Null(ValidadorArquivoPdf.Validar("manual.pdf", Pdf((int)DoisMb), DoisMb));
    }

    [Fact]
    public void Validar_PdfMaiorQue2Mb_RetornaErroDeTamanho()
    {
        var erro = ValidadorArquivoPdf.Validar("manual.pdf", Pdf((int)DoisMb + 1), DoisMb);

        Assert.Equal("O arquivo excede o tamanho máximo de 2 MB.", erro);
    }

    [Fact]
    public void Validar_ArquivoVazio_RetornaErro()
    {
        Assert.Equal("O arquivo enviado está vazio.", ValidadorArquivoPdf.Validar("manual.pdf", [], DoisMb));
    }

    [Fact]
    public void Validar_ExtensaoDiferenteDePdf_RetornaErro()
    {
        Assert.Equal("O arquivo enviado não é um PDF.", ValidadorArquivoPdf.Validar("manual.txt", Pdf(100), DoisMb));
    }

    [Fact]
    public void Validar_ConteudoSemAssinaturaPdf_RetornaErro()
    {
        var conteudo = Encoding.ASCII.GetBytes("não sou um pdf");

        Assert.Equal("O arquivo enviado não é um PDF.", ValidadorArquivoPdf.Validar("falso.pdf", conteudo, DoisMb));
    }

    private static byte[] Pdf(int tamanho)
    {
        var conteudo = new byte[tamanho];
        "%PDF-1.7"u8.CopyTo(conteudo);
        return conteudo;
    }
}
