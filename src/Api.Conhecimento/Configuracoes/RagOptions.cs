using System.ComponentModel.DataAnnotations;

namespace Api.Conhecimento.Configuracoes;

/// <summary>
/// Parâmetros da indexação e da busca de conhecimento.
/// </summary>
public class RagOptions
{
    public const string Secao = "Rag";

    /// <summary>
    /// Pasta com os arquivos .md, relativa à raiz do projeto (ou caminho absoluto).
    /// </summary>
    [Required]
    public string PastaDocumentos { get; set; } = "Documentos";

    /// <summary>
    /// Tamanho máximo de cada chunk, em caracteres.
    /// </summary>
    [Range(100, 8000)]
    public int TamanhoMaximoChunk { get; set; } = 800;

    /// <summary>
    /// Quantidade de chunks mais semelhantes devolvidos na busca.
    /// </summary>
    [Range(1, 20)]
    public int QuantidadeResultados { get; set; } = 3;
}
