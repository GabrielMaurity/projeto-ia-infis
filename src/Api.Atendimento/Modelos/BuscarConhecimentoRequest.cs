using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Modelos;

/// <summary>
/// Pedido de busca na base de conhecimento.
/// </summary>
public class BuscarConhecimentoRequest
{
    /// <summary>
    /// Pergunta ou termo a pesquisar.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(2000)]
    public string Pergunta { get; set; } = string.Empty;
}
