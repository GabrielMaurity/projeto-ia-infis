using Api.Conhecimento.Configuracoes;
using Api.Conhecimento.Rag;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Ingestao;

/// <summary>
/// Documento recebido para ingestão.
/// </summary>
public record SolicitacaoIngestao(
    string NomeUsuario,
    string CodigoDocumento,
    DateOnly Validade,
    string NomeArquivo,
    Stream Conteudo);

/// <summary>
/// Resultado da ingestão: status HTTP sugerido e mensagem para o cliente.
/// </summary>
public record ResultadoIngestao(int Status, string Mensagem, int QuantidadeChunks = 0);

public interface IIngestaoDocumentos
{
    Task<ResultadoIngestao> IngerirAsync(SolicitacaoIngestao solicitacao, CancellationToken cancellationToken);
}

/// <summary>
/// Valida o PDF, extrai o texto, divide em chunks, gera os embeddings e grava no banco RAG.
/// Reenviar um código de documento já cadastrado substitui os chunks anteriores.
/// </summary>
public sealed class IngestaoDocumentos(
    IExtratorTextoPdf extrator,
    IDivisorChunks divisor,
    IGeradorEmbeddings geradorEmbeddings,
    IRepositorioChunks repositorio,
    IOptions<IngestaoOptions> options,
    TimeProvider timeProvider,
    ILogger<IngestaoDocumentos> logger) : IIngestaoDocumentos
{
    public const string MensagemSucesso = "Conhecimento cadastrado com sucesso";

    public async Task<ResultadoIngestao> IngerirAsync(SolicitacaoIngestao solicitacao, CancellationToken cancellationToken)
    {
        var config = options.Value;
        var codigo = solicitacao.CodigoDocumento;

        var hoje = DateOnly.FromDateTime(timeProvider.GetLocalNow().Date);
        if (solicitacao.Validade < hoje)
        {
            return Falha(StatusCodes.Status400BadRequest, "A validade deve ser hoje ou uma data futura.");
        }

        logger.LogInformation("[{Codigo}] Passo 1/5: validando o arquivo {Arquivo}.", codigo, solicitacao.NomeArquivo);
        using var memoria = new MemoryStream();
        await solicitacao.Conteudo.CopyToAsync(memoria, cancellationToken);
        var pdf = memoria.ToArray();

        var erro = ValidadorArquivoPdf.Validar(solicitacao.NomeArquivo, pdf, config.TamanhoMaximoArquivoBytes);
        if (erro is not null)
        {
            return Falha(StatusCodes.Status400BadRequest, erro);
        }

        logger.LogInformation("[{Codigo}] Passo 2/5: extraindo o texto do PDF ({Bytes} bytes).", codigo, pdf.Length);
        string texto;
        try
        {
            texto = extrator.Extrair(pdf);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[{Codigo}] Falha ao ler o PDF.", codigo);
            return Falha(StatusCodes.Status422UnprocessableEntity,
                "Não foi possível ler o PDF. Verifique se o arquivo não está corrompido ou protegido por senha.");
        }

        if (string.IsNullOrWhiteSpace(texto))
        {
            return Falha(StatusCodes.Status422UnprocessableEntity,
                "O PDF não possui texto extraível (pode ser um documento escaneado).");
        }

        logger.LogInformation("[{Codigo}] Passo 3/5: dividindo {Caracteres} caractere(s) em chunks.", codigo, texto.Length);
        var chunks = divisor.Dividir(new Documento(solicitacao.NomeArquivo, texto));

        logger.LogInformation("[{Codigo}] Passo 4/5: gerando embeddings de {Chunks} chunk(s).", codigo, chunks.Count);
        IReadOnlyList<float[]> embeddings;
        try
        {
            embeddings = await geradorEmbeddings.GerarAsync(chunks.Select(c => c.Texto).ToList(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[{Codigo}] Falha ao gerar os embeddings.", codigo);
            return Falha(StatusCodes.Status502BadGateway,
                "Não foi possível gerar os embeddings do documento. Tente novamente mais tarde.");
        }

        var indexados = chunks.Zip(embeddings, (chunk, embedding) => new ChunkIndexado(chunk, embedding)).ToList();

        logger.LogInformation("[{Codigo}] Passo 5/5: gravando {Chunks} chunk(s) no banco RAG.", codigo, indexados.Count);
        var documento = new MetadadosDocumento(
            config.IdAgente, codigo, solicitacao.NomeArquivo, solicitacao.Validade, solicitacao.NomeUsuario);
        int removidos;
        try
        {
            removidos = await repositorio.SubstituirDocumentoAsync(documento, indexados, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[{Codigo}] Falha ao gravar os chunks no banco RAG.", codigo);
            return Falha(StatusCodes.Status503ServiceUnavailable,
                "Não foi possível gravar o documento na base de conhecimento. Tente novamente mais tarde.");
        }

        if (removidos > 0)
        {
            logger.LogInformation("[{Codigo}] {Removidos} chunk(s) da versão anterior do documento foram substituídos.",
                codigo, removidos);
        }

        logger.LogInformation("[{Codigo}] Ingestão concluída: {Chunks} chunk(s) gravado(s).", codigo, indexados.Count);

        return new ResultadoIngestao(StatusCodes.Status201Created, MensagemSucesso, indexados.Count);

        ResultadoIngestao Falha(int status, string mensagem)
        {
            logger.LogWarning("[{Codigo}] Ingestão recusada: {Mensagem}", codigo, mensagem);
            return new ResultadoIngestao(status, mensagem);
        }
    }
}
