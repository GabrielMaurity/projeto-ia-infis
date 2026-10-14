using System.Text.Json.Nodes;
using Api.Atendimento.Configuracoes;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Servicos.Llm;

/// <summary>
/// Gera respostas com o SDK oficial do Google (Google.GenAI), com o mesmo prompt e o mesmo
/// JSON Schema de saída usados na OpenAI. Usado somente como contingência.
/// </summary>
public sealed class GeminiLlmClient : ILlmClient
{
    private static readonly JsonNode Schema = JsonNode.Parse(SaidaEstruturada.JsonSchema)!;

    private readonly Lazy<Client> _client;
    private readonly IOptions<GeminiOptions> _options;
    private readonly ILogger<GeminiLlmClient> _logger;

    public GeminiLlmClient(IOptions<GeminiOptions> options, ILogger<GeminiLlmClient> logger)
    {
        _options = options;
        _logger = logger;

        // Criado sob demanda para a API subir mesmo sem a chave configurada.
        _client = new Lazy<Client>(() =>
        {
            var config = options.Value;
            if (string.IsNullOrWhiteSpace(config.ApiKey))
            {
                throw new InvalidOperationException(
                    "Gemini:ApiKey não configurada. Use user-secrets ou a variável de ambiente Gemini__ApiKey.");
            }

            return new Client(apiKey: config.ApiKey);
        });
    }

    public async Task<RespostaLlm> GerarRespostaAsync(string instrucoes, string mensagem, CancellationToken cancellationToken)
    {
        var config = _options.Value;

        var configuracao = new GenerateContentConfig
        {
            SystemInstruction = new Content { Parts = [new Part { Text = instrucoes }] },
            ResponseMimeType = "application/json",
            ResponseJsonSchema = Schema,
            // Respostas fiscais devem ser fiéis ao contexto, não criativas.
            Temperature = 0.2
        };

        var resposta = await _client.Value.Models.GenerateContentAsync(
            config.Modelo, mensagem, configuracao, cancellationToken);

        var saida = SaidaEstruturada.Ler(resposta.Text, "O Gemini");

        var tokensEntrada = resposta.UsageMetadata?.PromptTokenCount ?? 0;
        var tokensSaida = CalcularTokensSaida(
            resposta.UsageMetadata?.CandidatesTokenCount, resposta.UsageMetadata?.ThoughtsTokenCount);
        var custo = CalculadoraCusto.Calcular(
            tokensEntrada, tokensSaida, config.CustoEntradaPorMilhaoTokens, config.CustoSaidaPorMilhaoTokens);
        var modelo = string.IsNullOrWhiteSpace(resposta.ModelVersion) ? config.Modelo : resposta.ModelVersion;

        _logger.LogInformation(
            "Resposta gerada pelo Gemini com o modelo {Modelo}: {TokensEntrada} tokens de entrada, {TokensSaida} de saída, custo estimado US$ {Custo}.",
            modelo, tokensEntrada, tokensSaida, custo.Total);

        return new RespostaLlm(
            saida.Resposta,
            saida.Categoria,
            saida.Fontes,
            modelo,
            tokensEntrada,
            tokensSaida,
            custo,
            saida.EncaminharSuporteHumano,
            saida.ResumoEncaminhamento);
    }

    /// <summary>
    /// O Gemini cobra os tokens de raciocínio (thoughts) como tokens de saída.
    /// </summary>
    public static int CalcularTokensSaida(int? tokensResposta, int? tokensRaciocinio)
        => (tokensResposta ?? 0) + (tokensRaciocinio ?? 0);
}
