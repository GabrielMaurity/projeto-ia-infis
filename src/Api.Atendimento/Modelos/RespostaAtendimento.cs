namespace Api.Atendimento.Modelos;

/// <summary>
/// Resposta estruturada do atendimento.
/// </summary>
/// <param name="Resposta">Resposta gerada pelo modelo com base no contexto.</param>
/// <param name="Categoria">Categoria do assunto da pergunta.</param>
/// <param name="Fontes">Documentos usados na resposta.</param>
/// <param name="Modelo">Modelo que gerou a resposta.</param>
/// <param name="Fallback">Indica se a resposta veio do modelo de contingência.</param>
/// <param name="TokensEntrada">Tokens de entrada consumidos.</param>
/// <param name="TokensSaida">Tokens de saída consumidos.</param>
/// <param name="CustoEstimado">Custo estimado da chamada.</param>
/// <param name="EncaminharSuporteHumano">Indica que o atendimento foi encaminhado para o suporte humano.</param>
public record RespostaAtendimento(
    string Resposta,
    string Categoria,
    string[] Fontes,
    string Modelo,
    bool Fallback,
    int TokensEntrada,
    int TokensSaida,
    decimal CustoEstimado,
    bool EncaminharSuporteHumano);
