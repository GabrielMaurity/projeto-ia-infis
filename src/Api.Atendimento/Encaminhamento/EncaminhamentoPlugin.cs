using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Api.Atendimento.Encaminhamento;

/// <summary>
/// Plugin do Semantic Kernel com as ações que o agente de atendimento pode executar.
/// </summary>
public sealed class EncaminhamentoPlugin(
    IOutboxMensagens outbox,
    TimeProvider timeProvider,
    ILogger<EncaminhamentoPlugin> logger)
{
    public const string NomePlugin = "Atendimento";
    public const string NomeFuncao = "EncaminharParaSuporteHumano";

    /// <summary>
    /// Encaminha o atendimento para o suporte humano. Apenas grava a mensagem na outbox;
    /// a publicação na fila é feita depois, por um processo de envio.
    /// </summary>
    /// <returns>Id da mensagem gravada na outbox.</returns>
    [KernelFunction(NomeFuncao)]
    [Description("Encaminha o atendimento para o suporte humano, registrando uma mensagem na outbox com o resumo do atendimento.")]
    public async Task<long> EncaminharParaSuporteHumanoAsync(
        [Description("Nome do usuário atendido.")] string usuario,
        [Description("Resumo do atendimento para o atendente humano: necessidade do usuário, o que já foi respondido, motivo do encaminhamento e pendências.")] string descricao,
        CancellationToken cancellationToken = default)
    {
        var id = await outbox.GravarAsync(
            new MensagemOutbox(timeProvider.GetUtcNow().UtcDateTime, usuario, descricao),
            cancellationToken);

        logger.LogInformation("Atendimento do usuário encaminhado para o suporte humano (outbox {IdMensagem}).", id);

        return id;
    }
}
