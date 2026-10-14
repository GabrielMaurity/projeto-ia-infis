using Api.Atendimento.Configuracoes;
using Api.Atendimento.Modelos;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Api.Atendimento.Servicos;

/// <summary>
/// MCP Client da Api.Conhecimento. Registrado como singleton: a conexão é aberta
/// na primeira chamada e reaproveitada nas seguintes.
/// </summary>
public sealed class ConhecimentoMcpClient(
    IHttpClientFactory httpClientFactory,
    IOptions<ConhecimentoMcpOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<ConhecimentoMcpClient> logger) : IConhecimentoMcpClient, IAsyncDisposable
{
    public const string NomeHttpClient = "ConhecimentoMcp";
    private const string ToolBuscarConhecimento = "BuscarConhecimento";

    private readonly SemaphoreSlim _conexaoLock = new(1, 1);
    private McpClient? _client;

    public async Task<IReadOnlyList<FerramentaMcp>> ListarFerramentasAsync(CancellationToken cancellationToken)
    {
        var client = await ObterClientAsync(cancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

        logger.LogInformation("{Quantidade} ferramenta(s) descoberta(s) no MCP Server.", tools.Count);

        return tools.Select(t => new FerramentaMcp(t.Name, t.Description)).ToList();
    }

    public async Task<string> BuscarConhecimentoAsync(string pergunta, CancellationToken cancellationToken)
    {
        var client = await ObterClientAsync(cancellationToken);

        var resultado = await client.CallToolAsync(
            ToolBuscarConhecimento,
            new Dictionary<string, object?> { ["pergunta"] = pergunta },
            cancellationToken: cancellationToken);

        var texto = string.Join(
            Environment.NewLine,
            resultado.Content.OfType<TextContentBlock>().Select(c => c.Text));

        if (resultado.IsError == true)
        {
            throw new InvalidOperationException($"A tool {ToolBuscarConhecimento} retornou erro: {texto}");
        }

        return texto;
    }

    private async Task<McpClient> ObterClientAsync(CancellationToken cancellationToken)
    {
        if (_client is not null)
        {
            return _client;
        }

        await _conexaoLock.WaitAsync(cancellationToken);
        try
        {
            if (_client is null)
            {
                var endpoint = new Uri(options.Value.Endpoint);
                var transport = new HttpClientTransport(
                    new HttpClientTransportOptions { Endpoint = endpoint, Name = "Api.Conhecimento" },
                    httpClientFactory.CreateClient(NomeHttpClient),
                    loggerFactory,
                    ownsHttpClient: true);

                _client = await McpClient.CreateAsync(transport, loggerFactory: loggerFactory, cancellationToken: cancellationToken);

                logger.LogInformation("Conectado ao MCP Server em {Endpoint}.", endpoint);
            }

            return _client;
        }
        finally
        {
            _conexaoLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        _conexaoLock.Dispose();
    }
}
