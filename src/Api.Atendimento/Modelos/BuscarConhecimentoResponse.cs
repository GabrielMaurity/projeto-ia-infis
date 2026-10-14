namespace Api.Atendimento.Modelos;

/// <summary>
/// Resultado devolvido pela tool BuscarConhecimento.
/// </summary>
/// <param name="Conteudo">Texto retornado pelo MCP Server.</param>
public record BuscarConhecimentoResponse(string Conteudo);
