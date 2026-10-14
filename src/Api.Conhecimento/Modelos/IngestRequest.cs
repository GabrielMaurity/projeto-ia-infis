using System.ComponentModel.DataAnnotations;

namespace Api.Conhecimento.Modelos;

/// <summary>
/// Dados enviados (multipart/form-data) para cadastrar um documento na base de conhecimento.
/// </summary>
public class IngestRequest
{
    /// <summary>
    /// Nome do usuário que está cadastrando o documento.
    /// </summary>
    [Required(ErrorMessage = "Informe o nome do usuário.")]
    [StringLength(100, ErrorMessage = "O nome do usuário deve ter no máximo 100 caracteres.")]
    public string? NomeUsuario { get; set; }

    /// <summary>
    /// Código único do documento. Reenviar um código existente substitui o documento.
    /// </summary>
    [Required(ErrorMessage = "Informe o código do documento.")]
    [StringLength(100, ErrorMessage = "O código do documento deve ter no máximo 100 caracteres.")]
    public string? CodigoDocumento { get; set; }

    /// <summary>
    /// Data de validade do documento (formato yyyy-MM-dd).
    /// </summary>
    [Required(ErrorMessage = "Informe a validade do documento.")]
    public DateOnly? Validade { get; set; }

    /// <summary>
    /// Arquivo PDF com no máximo 2 MB.
    /// </summary>
    [Required(ErrorMessage = "Envie o arquivo PDF.")]
    public IFormFile? Arquivo { get; set; }
}
