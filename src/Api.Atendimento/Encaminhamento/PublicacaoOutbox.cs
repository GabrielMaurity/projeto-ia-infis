using System.Text.Json;
using Api.Atendimento.Configuracoes;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Encaminhamento;

/// <summary>
/// JSON publicado na fila para cada encaminhamento.
/// </summary>
/// <param name="Id">Id da OutboxMessage (também enviado como MessageId da mensagem).</param>
/// <param name="NomeUsuario">Usuário atendido.</param>
/// <param name="Descricao">Resumo do atendimento gerado pela IA.</param>
public record MensagemAtendimentoFila(long Id, string NomeUsuario, string Descricao);

public interface IProcessadorOutbox
{
    /// <summary>
    /// Publica um lote de mensagens pendentes e retorna quantas foram transmitidas.
    /// </summary>
    Task<int> PublicarPendentesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Lê as primeiras mensagens não transmitidas da outbox, publica cada uma na fila e marca como
/// transmitida. A marcação acontece depois da confirmação do broker; se ela falhar, a mensagem é
/// publicada de novo na próxima execução (entrega "pelo menos uma vez" — o Worker.Suporte trata
/// reentregas pela tabela InboxMessage).
/// </summary>
public sealed class ProcessadorOutbox(
    IOutboxMensagens outbox,
    IPublicadorFila publicador,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<ProcessadorOutbox> logger) : IProcessadorOutbox
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public async Task<int> PublicarPendentesAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;
        var pendentes = await outbox.ObterPendentesAsync(config.TamanhoLote, cancellationToken);

        var transmitidas = 0;
        foreach (var mensagem in pendentes)
        {
            try
            {
                var corpo = JsonSerializer.SerializeToUtf8Bytes(
                    new MensagemAtendimentoFila(mensagem.Id, mensagem.Usuario, mensagem.Descricao), OpcoesJson);

                await publicador.PublicarAsync(config.Fila, mensagem.Id.ToString(), corpo, cancellationToken);
                await outbox.MarcarTransmitidaAsync(
                    mensagem.Id, config.Fila, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

                transmitidas++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Interrompe o lote para manter a ordem; as restantes ficam para a próxima execução.
                logger.LogWarning(ex,
                    "Falha ao transmitir a mensagem {IdMensagem} da outbox para a fila {Fila}; nova tentativa na próxima execução.",
                    mensagem.Id, config.Fila);
                break;
            }
        }

        if (pendentes.Count > 0)
        {
            logger.LogInformation("{Transmitidas} de {Pendentes} mensagem(ns) da outbox transmitida(s) para a fila {Fila}.",
                transmitidas, pendentes.Count, config.Fila);
        }

        return transmitidas;
    }
}

/// <summary>
/// Executa a publicação da outbox ao iniciar e depois a cada <see cref="OutboxOptions.IntervaloSegundos"/>.
/// </summary>
public sealed class PublicacaoOutboxHostedService(
    IProcessadorOutbox processador,
    IOptions<OutboxOptions> options,
    ILogger<PublicacaoOutboxHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervaloSegundos));

        try
        {
            do
            {
                try
                {
                    await processador.PublicarPendentesAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Ex.: SQL Server indisponível. O serviço continua e tenta de novo no próximo ciclo.
                    logger.LogError(ex, "Falha na publicação da outbox.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
