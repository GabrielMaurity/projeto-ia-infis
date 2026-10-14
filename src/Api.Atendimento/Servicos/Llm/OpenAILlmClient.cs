using System.ClientModel;
using Api.Atendimento.Configuracoes;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Api.Atendimento.Servicos.Llm;

/// <summary>
/// Gera respostas com o SDK oficial da OpenAI usando Structured Output (JSON Schema estrito).
/// O modelo preenche resposta, categoria e fontes; modelo e tokens vêm dos metadados da chamada.
/// </summary>
public sealed class OpenAILlmClient : ILlmClient
{
    private static readonly BinaryData Schema = BinaryData.FromString(SaidaEstruturada.JsonSchema);

    private readonly Lazy<ChatClient> _client;
    private readonly IOptions<OpenAIOptions> _options;
    private readonly ILogger<OpenAILlmClient> _logger;

    public OpenAILlmClient(IOptions<OpenAIOptions> options, ILogger<OpenAILlmClient> logger)
    {
        _options = options;
        _logger = logger;

        // Criado sob demanda para a API subir mesmo sem a chave configurada.
        _client = new Lazy<ChatClient>(() =>
        {
            var config = options.Value;
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI:ApiKey não configurada. Use user-secrets ou a variável de ambiente OpenAI__ApiKey.");
            }

            // Timeout menor que o padrão (100 s) para o fallback não demorar quando a OpenAI não responde.
            return new ChatClient(
                config.ModeloChat,
                new ApiKeyCredential(config.ApiKey),
                new OpenAIClientOptions { NetworkTimeout = TimeSpan.FromSeconds(config.TimeoutSegundos) });
        });
    }

    public async Task<RespostaLlm> GerarRespostaAsync(string instrucoes, string mensagem, CancellationToken cancellationToken)
    {
        var opcoes = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                SaidaEstruturada.NomeSchema, Schema, jsonSchemaIsStrict: true),
            // Respostas fiscais devem ser fiéis ao contexto, não criativas.
            Temperature = 0.2f
        };

        ChatCompletion completion = await _client.Value.CompleteChatAsync(
            [new SystemChatMessage(instrucoes), new UserChatMessage(mensagem)], opcoes, cancellationToken);

        if (!string.IsNullOrEmpty(completion.Refusal))
        {
            throw new InvalidOperationException($"A OpenAI recusou a solicitação: {completion.Refusal}");
        }

        var saida = SaidaEstruturada.Ler(completion.Content.FirstOrDefault()?.Text, "A OpenAI");

        var config = _options.Value;
        var tokensEntrada = completion.Usage?.InputTokenCount ?? 0;
        var tokensSaida = completion.Usage?.OutputTokenCount ?? 0;
        var custo = CalculadoraCusto.Calcular(
            tokensEntrada, tokensSaida, config.CustoEntradaPorMilhaoTokens, config.CustoSaidaPorMilhaoTokens);

        _logger.LogInformation(
            "Resposta gerada pela OpenAI com o modelo {Modelo}: {TokensEntrada} tokens de entrada, {TokensSaida} de saída, custo estimado US$ {Custo}.",
            completion.Model, tokensEntrada, tokensSaida, custo.Total);

        return new RespostaLlm(
            saida.Resposta,
            saida.Categoria,
            saida.Fontes,
            completion.Model,
            tokensEntrada,
            tokensSaida,
            custo,
            saida.EncaminharSuporteHumano,
            saida.ResumoEncaminhamento);
    }
}
