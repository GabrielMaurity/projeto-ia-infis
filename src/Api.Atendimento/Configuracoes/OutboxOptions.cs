using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Configuracoes;

/// <summary>
/// Publicação periódica da outbox de encaminhamentos na fila do RabbitMQ.
/// </summary>
public class OutboxOptions
{
    public const string Secao = "Outbox";

    /// <summary>
    /// Intervalo entre as execuções da publicação.
    /// </summary>
    [Range(5, 3600)]
    public int IntervaloSegundos { get; set; } = 60;

    /// <summary>
    /// Quantidade máxima de mensagens publicadas por execução.
    /// </summary>
    [Range(1, 1000)]
    public int TamanhoLote { get; set; } = 5;

    /// <summary>
    /// Fila do RabbitMQ que recebe os encaminhamentos.
    /// </summary>
    [Required]
    public string Fila { get; set; } = "atendimentos";
}
