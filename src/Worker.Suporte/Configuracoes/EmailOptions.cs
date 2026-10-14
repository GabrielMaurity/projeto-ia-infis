using System.ComponentModel.DataAnnotations;

namespace Worker.Suporte.Configuracoes;

/// <summary>
/// Servidor SMTP (Mailhog em desenvolvimento) e endereços do e-mail de novo atendimento.
/// </summary>
public class EmailOptions
{
    public const string Secao = "Email";

    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Porta { get; set; } = 1025;

    [Required]
    [EmailAddress]
    public string Remetente { get; set; } = "worker-service@infis.com.br";

    [Required]
    [EmailAddress]
    public string Destinatario { get; set; } = "atendimento-suporte@infis.com.br";
}
