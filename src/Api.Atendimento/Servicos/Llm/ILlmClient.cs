namespace Api.Atendimento.Servicos.Llm;

/// <summary>
/// Resposta de um modelo de linguagem, já convertida do Structured Output.
/// </summary>
/// <param name="Resposta">Texto da resposta.</param>
/// <param name="Categoria">Categoria do assunto da pergunta.</param>
/// <param name="Fontes">Documentos do contexto usados na resposta.</param>
/// <param name="Modelo">Modelo que respondeu, como informado pelo provedor.</param>
/// <param name="TokensEntrada">Tokens de entrada informados pelo provedor.</param>
/// <param name="TokensSaida">Tokens de saída informados pelo provedor.</param>
/// <param name="Custo">Custo estimado da chamada em dólares (USD), pela tabela de preços configurada.</param>
/// <param name="EncaminharSuporteHumano">Indica que o atendimento deve ser encaminhado ao suporte humano.</param>
/// <param name="ResumoEncaminhamento">Resumo do atendimento para o atendente humano (vazio se não houver encaminhamento).</param>
/// <param name="Fallback">Indica que a resposta veio do modelo de contingência (Gemini).</param>
public record RespostaLlm(
    string Resposta,
    string Categoria,
    string[] Fontes,
    string Modelo,
    int TokensEntrada,
    int TokensSaida,
    CustoChamada Custo,
    bool EncaminharSuporteHumano = false,
    string ResumoEncaminhamento = "",
    bool Fallback = false);

/// <summary>
/// Cliente de um modelo de linguagem que devolve resposta estruturada.
/// </summary>
public interface ILlmClient
{
    /// <param name="instrucoes">Mensagem de sistema (papel e regras do assistente).</param>
    /// <param name="mensagem">Mensagem do usuário (contexto e pergunta).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<RespostaLlm> GerarRespostaAsync(string instrucoes, string mensagem, CancellationToken cancellationToken);
}
