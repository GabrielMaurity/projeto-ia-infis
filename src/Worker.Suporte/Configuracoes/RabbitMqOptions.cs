using System.ComponentModel.DataAnnotations;

namespace Worker.Suporte.Configuracoes;

/// <summary>
/// Conexão com o RabbitMQ e fila consumida.
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

    /// <summary>
    /// Fila com os encaminhamentos publicados pela Api.Atendimento.
    /// </summary>
    [Required]
    public string Fila { get; set; } = "atendimentos";

    /// <summary>
    /// Espera antes de devolver à fila uma mensagem cujo processamento falhou (ex.: SMTP fora do ar).
    /// </summary>
    [Range(0, 600)]
    public int EsperaReprocessamentoSegundos { get; set; } = 15;
}
