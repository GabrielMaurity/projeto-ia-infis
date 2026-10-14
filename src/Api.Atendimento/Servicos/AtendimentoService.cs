using Api.Atendimento.Configuracoes;
using Api.Atendimento.Custos;
using Api.Atendimento.Encaminhamento;
using Api.Atendimento.Historico;
using Api.Atendimento.Modelos;
using Api.Atendimento.Servicos.Llm;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Api.Atendimento.Servicos;

public interface IAtendimentoService
{
    Task<RespostaAtendimento> ResponderAsync(string nomeUsuario, string pergunta, CancellationToken cancellationToken);
}

/// <summary>
/// Orquestra o atendimento: recupera as últimas conversas do usuário (MongoDB), busca o contexto
/// na Api.Conhecimento via MCP (busca vetorial no banco RAG), consulta o modelo de linguagem,
/// grava a nova interação no histórico, registra o consumo de tokens (SQL Server) e, quando o
/// modelo indicar, encaminha o atendimento para o suporte humano (função do Semantic Kernel).
/// </summary>
public sealed class AtendimentoService(
    IConhecimentoMcpClient conhecimentoMcpClient,
    ILlmClient llmClient,
    IHistoricoAtendimentos historico,
    IRegistroConsumoTokens registroConsumo,
    Kernel kernel,
    IOptions<AtendimentoOptions> options,
    TimeProvider timeProvider,
    ILogger<AtendimentoService> logger) : IAtendimentoService
{
    public async Task<RespostaAtendimento> ResponderAsync(
        string nomeUsuario,
        string pergunta,
        CancellationToken cancellationToken)
    {
        var agente = options.Value.Agente;

        // Histórico e contexto são independentes: buscados em paralelo.
        var tarefaHistorico = ObterHistoricoAsync(nomeUsuario, agente, cancellationToken);
        var contexto = await conhecimentoMcpClient.BuscarConhecimentoAsync(pergunta, cancellationToken);
        var conversasAnteriores = await tarefaHistorico;

        var mensagem = PromptAtendimento.MontarMensagem(nomeUsuario, conversasAnteriores, contexto, pergunta);

        var resposta = await llmClient.GerarRespostaAsync(PromptAtendimento.Instrucoes, mensagem, cancellationToken);

        logger.LogInformation(
            "Atendimento respondido pelo modelo {Modelo} (fallback: {Fallback}) na categoria {Categoria} com {Conversas} conversa(s) de histórico ({TokensEntrada}+{TokensSaida} tokens, US$ {Custo}).",
            resposta.Modelo, resposta.Fallback, resposta.Categoria, conversasAnteriores.Count,
            resposta.TokensEntrada, resposta.TokensSaida, resposta.Custo.Total);

        var resultado = new RespostaAtendimento(
            resposta.Resposta,
            resposta.Categoria,
            resposta.Fontes,
            resposta.Modelo,
            resposta.Fallback,
            resposta.TokensEntrada,
            resposta.TokensSaida,
            resposta.Custo.Total,
            resposta.EncaminharSuporteHumano);

        // A conversa é gravada primeiro para que o consumo referencie o seu Id no MongoDB.
        var dataHora = timeProvider.GetUtcNow().UtcDateTime;
        var idConversa = await GravarHistoricoAsync(dataHora, nomeUsuario, agente, pergunta, resultado);
        await RegistrarConsumoAsync(dataHora, nomeUsuario, agente, resposta, idConversa);

        if (resposta.EncaminharSuporteHumano)
        {
            await EncaminharParaSuporteHumanoAsync(nomeUsuario, pergunta, resposta);
        }

        return resultado;
    }

    /// <summary>
    /// Executa a função do Semantic Kernel que registra o encaminhamento na outbox.
    /// Mesmo critério das demais gravações: a falha é registrada no log (com o resumo, para
    /// permitir o encaminhamento manual) e não impede a entrega da resposta.
    /// </summary>
    private async Task EncaminharParaSuporteHumanoAsync(string nomeUsuario, string pergunta, RespostaLlm resposta)
    {
        // Se o modelo marcou o encaminhamento sem escrever o resumo, o atendente recebe ao menos a interação.
        var descricao = string.IsNullOrWhiteSpace(resposta.ResumoEncaminhamento)
            ? $"Pergunta do usuário: {pergunta}{Environment.NewLine}Resposta do assistente: {resposta.Resposta}"
            : resposta.ResumoEncaminhamento;

        try
        {
            await kernel.InvokeAsync(
                EncaminhamentoPlugin.NomePlugin,
                EncaminhamentoPlugin.NomeFuncao,
                new KernelArguments
                {
                    ["usuario"] = nomeUsuario,
                    ["descricao"] = descricao
                },
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Não foi possível encaminhar o atendimento para o suporte humano. Resumo: {Descricao}", descricao);
        }
    }

    /// <summary>
    /// Sem histórico o atendimento continua funcionando: uma falha no MongoDB só é registrada no log.
    /// </summary>
    private async Task<IReadOnlyList<ConversaAtendimento>> ObterHistoricoAsync(
        string nomeUsuario,
        string agente,
        CancellationToken cancellationToken)
    {
        var quantidade = options.Value.QuantidadeConversasHistorico;
        if (quantidade == 0)
        {
            return [];
        }

        try
        {
            return await historico.ObterUltimasAsync(nomeUsuario, agente, quantidade, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Não foi possível recuperar o histórico do usuário; respondendo sem histórico.");
            return [];
        }
    }

    /// <summary>
    /// A resposta já foi gerada (e paga): uma falha ao gravar não deve impedir sua entrega.
    /// Grava sem o token da requisição para não perder a conversa se o cliente desconectar.
    /// Retorna o Id da conversa no MongoDB, ou <c>null</c> se a gravação falhou.
    /// </summary>
    private async Task<string?> GravarHistoricoAsync(
        DateTime dataHora,
        string nomeUsuario,
        string agente,
        string pergunta,
        RespostaAtendimento resposta)
    {
        var conversa = new ConversaAtendimento
        {
            DataHora = dataHora,
            Usuario = nomeUsuario,
            Agente = agente,
            Pergunta = pergunta,
            RespostaEstruturada = RespostaEstruturada.De(resposta),
            RespostaTexto = resposta.Resposta
        };

        try
        {
            await historico.GravarAsync(conversa, CancellationToken.None);
            return conversa.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Não foi possível gravar a conversa no histórico de atendimentos.");
            return null;
        }
    }

    /// <summary>
    /// Mesmo critério do histórico: a falha no controle de custos é registrada no log e não
    /// impede a entrega da resposta.
    /// </summary>
    private async Task RegistrarConsumoAsync(
        DateTime dataHora,
        string nomeUsuario,
        string agente,
        RespostaLlm resposta,
        string? idConversa)
    {
        try
        {
            await registroConsumo.RegistrarAsync(new ConsumoTokens(
                agente,
                resposta.Modelo,
                nomeUsuario,
                dataHora,
                resposta.TokensEntrada,
                resposta.TokensSaida,
                resposta.Custo.Entrada,
                resposta.Custo.Saida,
                idConversa), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Não foi possível registrar o consumo de tokens ({TokensEntrada}+{TokensSaida}, US$ {Custo}) da conversa {IdConversa}.",
                resposta.TokensEntrada, resposta.TokensSaida, resposta.Custo.Total, idConversa);
        }
    }
}
