namespace Api.Atendimento.Modelos;

/// <summary>
/// Ferramenta descoberta no MCP Server.
/// </summary>
/// <param name="Nome">Nome da ferramenta.</param>
/// <param name="Descricao">Descrição informada pelo servidor.</param>
public record FerramentaMcp(string Nome, string? Descricao);
