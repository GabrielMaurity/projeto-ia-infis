using Api.Conhecimento.Configuracoes;
using Api.Conhecimento.Ingestao;
using Api.Conhecimento.Rag;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Tests.Ingestao;

public class IngestaoDocumentosTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 8);

    private readonly ExtratorFalso _extrator = new();
    private readonly GeradorEmbeddingsFalso _gerador = new();
    private readonly RepositorioFalso _repositorio = new();

    [Fact]
    public async Task IngerirAsync_PdfValido_GravaChunksComMetadadosERetorna201()
    {
        _extrator.Texto = "Primeiro parágrafo.\n\nSegundo parágrafo.";

        var resultado = await Ingestao().IngerirAsync(Solicitacao(Hoje.AddDays(30)), CancellationToken.None);

        Assert.Equal(StatusCodes.Status201Created, resultado.Status);
        Assert.Equal("Conhecimento cadastrado com sucesso", resultado.Mensagem);
        Assert.Equal(
            new MetadadosDocumento("agente-teste", "DOC-1", "manual.pdf", Hoje.AddDays(30), "maria"),
            _repositorio.Documento);
        var gravado = Assert.Single(_repositorio.Chunks!);
        Assert.Equal("Primeiro parágrafo.\n\nSegundo parágrafo.", gravado.Chunk.Texto);
        Assert.Equal([gravado.Chunk.Texto.Length], gravado.Embedding);
    }

    [Fact]
    public async Task IngerirAsync_ValidadeNoPassado_Retorna400SemGravar()
    {
        var resultado = await Ingestao().IngerirAsync(Solicitacao(Hoje.AddDays(-1)), CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, resultado.Status);
        Assert.Null(_repositorio.Documento);
    }

    [Fact]
    public async Task IngerirAsync_ArquivoQueNaoEPdf_Retorna400SemGravar()
    {
        var resultado = await Ingestao().IngerirAsync(
            Solicitacao(Hoje, conteudo: "texto"u8.ToArray()), CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, resultado.Status);
        Assert.Equal("O arquivo enviado não é um PDF.", resultado.Mensagem);
        Assert.Null(_repositorio.Documento);
    }

    [Fact]
    public async Task IngerirAsync_PdfSemTexto_Retorna422SemGravar()
    {
        _extrator.Texto = "  ";

        var resultado = await Ingestao().IngerirAsync(Solicitacao(Hoje), CancellationToken.None);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, resultado.Status);
        Assert.Null(_repositorio.Documento);
    }

    [Fact]
    public async Task IngerirAsync_PdfIlegivel_Retorna422SemGravar()
    {
        _extrator.Erro = new InvalidOperationException("PDF corrompido");

        var resultado = await Ingestao().IngerirAsync(Solicitacao(Hoje), CancellationToken.None);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, resultado.Status);
        Assert.Null(_repositorio.Documento);
    }

    [Fact]
    public async Task IngerirAsync_FalhaNosEmbeddings_Retorna502SemGravar()
    {
        _gerador.Erro = new InvalidOperationException("OpenAI fora do ar");

        var resultado = await Ingestao().IngerirAsync(Solicitacao(Hoje), CancellationToken.None);

        Assert.Equal(StatusCodes.Status502BadGateway, resultado.Status);
        Assert.Null(_repositorio.Documento);
    }

    [Fact]
    public async Task IngerirAsync_FalhaNoBanco_Retorna503()
    {
        _repositorio.Erro = new InvalidOperationException("banco fora do ar");

        var resultado = await Ingestao().IngerirAsync(Solicitacao(Hoje), CancellationToken.None);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, resultado.Status);
    }

    private IngestaoDocumentos Ingestao()
        => new(
            _extrator,
            new DivisorChunks(Options.Create(new RagOptions())),
            _gerador,
            _repositorio,
            Options.Create(new IngestaoOptions { IdAgente = "agente-teste" }),
            new RelogioFixo(Hoje),
            NullLogger<IngestaoDocumentos>.Instance);

    private static SolicitacaoIngestao Solicitacao(DateOnly validade, byte[]? conteudo = null)
        => new("maria", "DOC-1", validade, "manual.pdf", new MemoryStream(conteudo ?? "%PDF-1.7 conteudo"u8.ToArray()));

    private sealed class ExtratorFalso : IExtratorTextoPdf
    {
        public string Texto { get; set; } = "texto";

        public Exception? Erro { get; set; }

        public string Extrair(byte[] pdf) => Erro is null ? Texto : throw Erro;
    }

    /// <summary>
    /// Embedding de uma dimensão com o tamanho do texto, para conferir a associação chunk ↔ embedding.
    /// </summary>
    private sealed class GeradorEmbeddingsFalso : IGeradorEmbeddings
    {
        public Exception? Erro { get; set; }

        public Task<IReadOnlyList<float[]>> GerarAsync(IReadOnlyList<string> textos, CancellationToken cancellationToken)
            => Erro is null
                ? Task.FromResult<IReadOnlyList<float[]>>(textos.Select(t => new float[] { t.Length }).ToList())
                : throw Erro;
    }

    private sealed class RepositorioFalso : IRepositorioChunks
    {
        public MetadadosDocumento? Documento { get; private set; }

        public IReadOnlyList<ChunkIndexado>? Chunks { get; private set; }

        public Exception? Erro { get; set; }

        public Task<int> SubstituirDocumentoAsync(
            MetadadosDocumento documento, IReadOnlyList<ChunkIndexado> chunks, CancellationToken cancellationToken)
        {
            if (Erro is not null)
            {
                throw Erro;
            }

            Documento = documento;
            Chunks = chunks;
            return Task.FromResult(0);
        }

        public Task<IReadOnlyList<TrechoEncontrado>> BuscarMaisSimilaresAsync(
            string idAgente, float[] consulta, int quantidade, DateOnly dataReferencia, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class RelogioFixo(DateOnly hoje) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(hoje.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
