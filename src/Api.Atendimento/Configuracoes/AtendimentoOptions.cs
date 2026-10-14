using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Configuracoes;

/// <summary>
/// Parâmetros do agente de atendimento.
/// </summary>
public class AtendimentoOptions
{
    public const string Secao = "Atendimento";

    /// <summary>
    /// Identificador do agente, gravado em cada conversa do histórico.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Agente { get; set; } = "assistente-tributario-contabil";

    /// <summary>
    /// Quantidade de conversas anteriores do usuário enviadas ao modelo (0 desliga o histórico).
    /// </summary>
    [Range(0, 20)]
    public int QuantidadeConversasHistorico { get; set; } = 3;
}
