using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Worker.Suporte.Configuracoes;
using Worker.Suporte.Processamento;

namespace Worker.Suporte;

/// <summary>
/// Consome a fila de atendimentos do RabbitMQ com confirmação manual: ack depois do processamento,
/// nack com nova tentativa em falhas temporárias e reject para mensagens inválidas. Uma mensagem por
/// vez (prefetch 1). Enquanto o RabbitMQ não estiver disponível na subida, tenta conectar de novo;
/// depois de conectado, a reconexão é feita pela recuperação automática do cliente.
/// </summary>
public sealed class ConsumidorFilaAtendimentos(
    IProcessadorMensagemSuporte processador,
    IOptions<RabbitMqOptions> options,
    ILogger<ConsumidorFilaAtendimentos> logger) : BackgroundService
{
    private static readonly TimeSpan EsperaReconexao = TimeSpan.FromSeconds(10);

    private IConnection? _conexao;
    private IChannel? _canal;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConectarAsync(config, stoppingToken);
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Não foi possível conectar ao RabbitMQ em {Host}:{Porta}; nova tentativa em {Segundos}s.",
                    config.Host, config.Porta, EsperaReconexao.TotalSeconds);
                await FecharAsync();
                await Task.Delay(EsperaReconexao, stoppingToken);
            }
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ConectarAsync(RabbitMqOptions config, CancellationToken stoppingToken)
    {
        var fabrica = new ConnectionFactory
        {
            HostName = config.Host,
            Port = config.Porta,
            UserName = config.Usuario,
            Password = config.Senha,
            ClientProvidedName = "Worker.Suporte"
        };

        _conexao = await fabrica.CreateConnectionAsync(stoppingToken);
        _canal = await _conexao.CreateChannelAsync(cancellationToken: stoppingToken);

        // Mesmos parâmetros usados pela Api.Atendimento ao publicar.
        await _canal.QueueDeclareAsync(config.Fila, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);
        await _canal.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

        var consumidor = new AsyncEventingBasicConsumer(_canal);
        consumidor.ReceivedAsync += (_, entrega) => TratarEntregaAsync(entrega, config, stoppingToken);

        await _canal.BasicConsumeAsync(config.Fila, autoAck: false, consumidor, stoppingToken);

        logger.LogInformation("Consumindo a fila {Fila} do RabbitMQ em {Host}:{Porta}.", config.Fila, config.Host, config.Porta);
    }

    private async Task TratarEntregaAsync(BasicDeliverEventArgs entrega, RabbitMqOptions config, CancellationToken stoppingToken)
    {
        var canal = _canal!;
        ResultadoProcessamento resultado;

        try
        {
            resultado = await processador.ProcessarAsync(
                entrega.BasicProperties.MessageId, entrega.Body, config.Fila, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Encerrando: a mensagem não confirmada volta para a fila automaticamente.
            return;
        }
        catch (Exception ex)
        {
            // Ex.: SQL Server indisponível ao gravar a InboxMessage.
            logger.LogError(ex, "Falha ao processar a mensagem {IdMensagem}; ela voltará para a fila.",
                entrega.BasicProperties.MessageId);
            resultado = ResultadoProcessamento.Reprocessar;
        }

        switch (resultado)
        {
            case ResultadoProcessamento.Concluido:
                await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, stoppingToken);
                break;

            case ResultadoProcessamento.Descartar:
                await canal.BasicRejectAsync(entrega.DeliveryTag, requeue: false, stoppingToken);
                break;

            default:
                // Espera antes de devolver para não repetir a falha em sequência.
                await Task.Delay(TimeSpan.FromSeconds(config.EsperaReprocessamentoSegundos), stoppingToken);
                await canal.BasicNackAsync(entrega.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                break;
        }
    }

    private async Task FecharAsync()
    {
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FecharAsync();
    }
}
