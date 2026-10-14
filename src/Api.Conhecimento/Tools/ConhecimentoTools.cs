using System.ComponentModel;
using System.Globalization;
using System.Text;
using Api.Conhecimento.Rag;
using ModelContextProtocol.Server;

namespace Api.Conhecimento.Tools;

/// <summary>
/// Ferramentas MCP expostas pela API de conhecimento.
/// </summary>
[McpServerToolType]
public class ConhecimentoTools(IBuscaConhecimento buscaConhecimento, ILogger<ConhecimentoTools> logger)
{
    /// <summary>
    /// Busca os trechos da base de conhecimento corporativa mais semelhantes à pergunta.
    /// </summary>
    [McpServerTool(Name = "BuscarConhecimento")]
    [Description("Busca na base de conhecimento corporativa os trechos mais relevantes para a pergunta informada.")]
    public async Task<string> BuscarConhecimentoAsync(
        [Description("Pergunta ou termo a ser pesquisado na base de conhecimento.")] string pergunta,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("BuscarConhecimento chamada com pergunta de {Tamanho} caracteres.", pergunta.Length);

        var trechos = await buscaConhecimento.BuscarAsync(pergunta, cancellationToken);

        if (trechos.Count == 0)
        {
            return "Nenhum conhecimento encontrado para a pergunta.";
        }

        var resposta = new StringBuilder();
        for (var i = 0; i < trechos.Count; i++)
        {
            var trecho = trechos[i];
            resposta.AppendLine(CultureInfo.InvariantCulture,
                $"[Trecho {i + 1}] Fonte: {trecho.Chunk.Fonte} | Similaridade: {trecho.Similaridade:F4}");
            resposta.AppendLine(trecho.Chunk.Texto);
            resposta.AppendLine();
        }

        return resposta.ToString().TrimEnd();
    }
}
