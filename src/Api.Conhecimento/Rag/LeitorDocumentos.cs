using Api.Conhecimento.Configuracoes;
using Microsoft.Extensions.Options;

namespace Api.Conhecimento.Rag;

/// <summary>
/// Documento carregado da pasta de documentos.
/// </summary>
public record Documento(string Nome, string Conteudo);

public interface ILeitorDocumentos
{
    Task<IReadOnlyList<Documento>> LerAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Lê os arquivos .md da pasta configurada em <see cref="RagOptions.PastaDocumentos"/>.
/// </summary>
public sealed class LeitorDocumentos(
    IOptions<RagOptions> options,
    IHostEnvironment environment,
    ILogger<LeitorDocumentos> logger) : ILeitorDocumentos
{
    public async Task<IReadOnlyList<Documento>> LerAsync(CancellationToken cancellationToken)
    {
        var pasta = Path.Combine(environment.ContentRootPath, options.Value.PastaDocumentos);

        if (!Directory.Exists(pasta))
        {
            logger.LogWarning("Pasta de documentos não encontrada: {Pasta}.", pasta);
            return [];
        }

        var documentos = new List<Documento>();

        foreach (var arquivo in Directory.EnumerateFiles(pasta, "*.md", SearchOption.AllDirectories).Order())
        {
            var conteudo = await File.ReadAllTextAsync(arquivo, cancellationToken);
            documentos.Add(new Documento(Path.GetRelativePath(pasta, arquivo), conteudo));
        }

        logger.LogInformation("{Quantidade} documento(s) .md lido(s) de {Pasta}.", documentos.Count, pasta);

        return documentos;
    }
}
