using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Modelos;

/// <summary>
/// Novo atendimento: dúvida tributária ou contábil de um usuário.
/// </summary>
public class AtendimentoRequest
{
    /// <summary>
    /// Nome do usuário que está sendo atendido.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(100)]
    public string NomeUsuario { get; set; } = string.Empty;

    /// <summary>
    /// Pergunta do usuário.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(2000)]
    public string Pergunta { get; set; } = string.Empty;
}
