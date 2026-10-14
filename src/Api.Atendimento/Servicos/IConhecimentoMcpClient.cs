using Api.Atendimento.Modelos;

namespace Api.Atendimento.Servicos;

/// <summary>
/// Acesso às ferramentas MCP expostas pela Api.Conhecimento.
/// </summary>
public interface IConhecimentoMcpClient
{
    /// <summary>
    /// Lista as ferramentas disponíveis no MCP Server.
    /// </summary>
    Task<IReadOnlyList<FerramentaMcp>> ListarFerramentasAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Chama a tool BuscarConhecimento e devolve o texto retornado.
    /// </summary>
    Task<string> BuscarConhecimentoAsync(string pergunta, CancellationToken cancellationToken);
}
