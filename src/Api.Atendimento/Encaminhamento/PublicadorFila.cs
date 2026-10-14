using Api.Atendimento.Configuracoes;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Api.Atendimento.Encaminhamento;

public interface IPublicadorFila
{
    /// <summary>
    /// Publica a mensagem na fila e só retorna depois da confirmação do broker.
    /// </summary>
    Task PublicarAsync(string fila, string idMensagem, ReadOnlyMemory<byte> corpoJson, CancellationToken cancellationToken);
}

/// <summary>
/// Publica no RabbitMQ (exchange padrão, routing key = nome da fila) com publisher confirms e
/// mensagens persistentes. A conexão e o canal são abertos na primeira publicação e recriados
/// depois de uma falha.
/// </summary>
public sealed class PublicadorRabbitMq(IOptions<RabbitMqOptions> options, ILogger<PublicadorRabbitMq> logger)
    : IPublicadorFila, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly HashSet<string> _filasDeclaradas = [];
    private IConnection? _conexao;
    private IChannel? _canal;

    public async Task PublicarAsync(string fila, string idMensagem, ReadOnlyMemory<byte> corpoJson, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var canal = await ObterCanalAsync(cancellationToken);

            if (_filasDeclaradas.Add(fila))
            {
                // Idempotente: o Worker.Suporte declara a mesma fila com os mesmos parâmetros.
                await canal.QueueDeclareAsync(fila, durable: true, exclusive: false, autoDelete: false,
                    cancellationToken: cancellationToken);
            }

            var propriedades = new BasicProperties
            {
                MessageId = idMensagem,
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                DeliveryMode = DeliveryModes.Persistent
            };

            // mandatory: falha se a mensagem não puder ser roteada para nenhuma fila.
            await canal.BasicPublishAsync(string.Empty, fila, mandatory: true, propriedades, corpoJson, cancellationToken);
        }
        catch
        {
            await FecharAsync();
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> ObterCanalAsync(CancellationToken cancellationToken)
    {
        if (_canal is { IsOpen: true })
        {
            return _canal;
        }

        await FecharAsync();

        var config = options.Value;
        var fabrica = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Porta,
            UserName = config.Usuario,
            Password = config.Senha,
            ClientProvidedName = "Api.Atendimento"
        };

        _conexao = await fabrica.CreateConnectionAsync(cancellationToken);
        _canal = await _conexao.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        logger.LogInformation("Conectado ao RabbitMQ em {Host}:{Porta}.", config.Host, config.Porta);

        return _canal;
    }

    private async Task FecharAsync()
    {
        _filasDeclaradas.Clear();

        if (_canal is not null)
        {
            await _canal.DisposeAsync();
            _canal = null;
        }

        if (_conexao is not null)
        {
            await _conexao.DisposeAsync();
            _conexao = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await FecharAsync();
        _lock.Dispose();
    }
}
