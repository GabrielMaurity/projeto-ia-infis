using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Configuracoes;

/// <summary>
/// Conexão com o RabbitMQ, para onde a outbox de encaminhamentos é publicada.
/// </summary>
public class RabbitMqOptions
{
    public const string Secao = "RabbitMq";

    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Porta { get; set; } = 5672;

    [Required]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}
