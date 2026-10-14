using Worker.Suporte.Email;
using Worker.Suporte.Inbox;
using Worker.Suporte.Mensagens;

namespace Worker.Suporte.Processamento;

/// <summary>
/// O que fazer com a mensagem na fila depois do processamento.
/// </summary>
public enum ResultadoProcessamento
{
    /// <summary>Processada (ou duplicada já tratada): confirmar (ack).</summary>
    Concluido,

    /// <summary>Falha temporária (ex.: SMTP fora do ar): devolver à fila para nova tentativa.</summary>
    Reprocessar,

    /// <summary>Mensagem inválida: descartar sem devolver à fila.</summary>
    Descartar
}

public interface IProcessadorMensagemSuporte
{
    Task<ResultadoProcessamento> ProcessarAsync(
        string? idMensagemFila,
        ReadOnlyMemory<byte> corpo,
        string nomeFila,
        CancellationToken cancellationToken);
}

/// <summary>
/// Para cada mensagem lida da fila: grava a leitura na InboxMessage, envia o e-mail para o suporte
/// e atualiza o status do envio. Uma reentrega de mensagem cujo e-mail já foi enviado é registrada
/// como "Duplicada" e não gera outro e-mail.
/// </summary>
public sealed class ProcessadorMensagemSuporte(
    IInboxMensagens inbox,
    IEnviadorEmail enviadorEmail,
    TimeProvider timeProvider,
    ILogger<ProcessadorMensagemSuporte> logger) : IProcessadorMensagemSuporte
{
    public async Task<ResultadoProcessamento> ProcessarAsync(
        string? idMensagemFila,
        ReadOnlyMemory<byte> corpo,
        string nomeFila,
        CancellationToken cancellationToken)
    {
        var mensagem = MensagemAtendimento.Ler(corpo.Span);
        if (mensagem is null)
        {
            logger.LogError("Mensagem {IdMensagem} da fila {Fila} descartada: JSON inválido ou incompleto.",
                idMensagemFila, nomeFila);
            return ResultadoProcessamento.Descartar;
        }

        var id = string.IsNullOrWhiteSpace(idMensagemFila) ? mensagem.Id.ToString() : idMensagemFila;
        var agora = timeProvider.GetUtcNow().UtcDateTime;

        var jaEnviado = await inbox.EmailJaEnviadoAsync(id, cancellationToken);
        var idInbox = await inbox.RegistrarAsync(
            new RegistroInbox(id, agora, nomeFila, mensagem.NomeUsuario, mensagem.Descricao,
                jaEnviado ? StatusEnvioEmail.Duplicada : StatusEnvioEmail.Pendente),
            cancellationToken);

        if (jaEnviado)
        {
            logger.LogInformation("Mensagem {IdMensagem} já teve o e-mail enviado; reentrega registrada como duplicada.", id);
            return ResultadoProcessamento.Concluido;
        }

        try
        {
            await enviadorEmail.EnviarAsync(ModeloEmailSuporte.Montar(mensagem, agora), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao enviar o e-mail da mensagem {IdMensagem}; ela voltará para a fila.", id);
            await inbox.AtualizarStatusAsync(idInbox, StatusEnvioEmail.Falha, CancellationToken.None);
            return ResultadoProcessamento.Reprocessar;
        }

        await inbox.AtualizarStatusAsync(idInbox, StatusEnvioEmail.Enviado, CancellationToken.None);
        logger.LogInformation("E-mail do atendimento {IdMensagem} enviado para o suporte.", id);

        return ResultadoProcessamento.Concluido;
    }
}
