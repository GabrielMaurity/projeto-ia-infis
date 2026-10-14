using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Configuracoes;

/// <summary>
/// Configuração de acesso ao MCP Server da Api.Conhecimento.
/// </summary>
public class ConhecimentoMcpOptions
{
    public const string Secao = "ConhecimentoMcp";

    /// <summary>
    /// URL completa do endpoint MCP (ex.: http://localhost:5026/mcp).
    /// </summary>
    [Required]
    [Url]
    public string Endpoint { get; set; } = string.Empty;
}
