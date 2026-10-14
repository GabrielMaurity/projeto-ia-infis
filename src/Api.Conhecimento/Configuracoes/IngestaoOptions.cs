using System.ComponentModel.DataAnnotations;

namespace Api.Conhecimento.Configuracoes;

/// <summary>
/// Parâmetros da ingestão de documentos enviados pelo endpoint de ingest.
/// </summary>
public class IngestaoOptions
{
    public const string Secao = "Ingestao";

    /// <summary>
    /// Agente ao qual os documentos ingeridos ficam associados.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string IdAgente { get; set; } = "agente-corporativo";

    /// <summary>
    /// Tamanho máximo do arquivo PDF, em bytes (padrão: 2 MB).
    /// </summary>
    [Range(1, 10 * 1024 * 1024)]
    public long TamanhoMaximoArquivoBytes { get; set; } = 2 * 1024 * 1024;
}
