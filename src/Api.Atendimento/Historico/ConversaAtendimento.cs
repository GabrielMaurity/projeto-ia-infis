using Api.Atendimento.Modelos;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Atendimento.Historico;

/// <summary>
/// Uma interação do usuário com o atendimento, gravada na collection de histórico.
/// </summary>
public class ConversaAtendimento
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Data e hora (UTC) em que a resposta foi gerada.
    /// </summary>
    public DateTime DataHora { get; set; }

    /// <summary>
    /// Nome do usuário da conversa.
    /// </summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>
    /// Agente que atendeu.
    /// </summary>
    public string Agente { get; set; } = string.Empty;

    public string Pergunta { get; set; } = string.Empty;

    /// <summary>
    /// Resposta estruturada completa devolvida pela API.
    /// </summary>
    public RespostaEstruturada RespostaEstruturada { get; set; } = new();

    /// <summary>
    /// Somente o texto da resposta (campo resposta).
    /// </summary>
    public string RespostaTexto { get; set; } = string.Empty;
}

/// <summary>
/// Cópia persistida de <see cref="RespostaAtendimento"/>.
/// </summary>
public class RespostaEstruturada
{
    public string Resposta { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public string[] Fontes { get; set; } = [];

    public string Modelo { get; set; } = string.Empty;

    public bool Fallback { get; set; }

    public int TokensEntrada { get; set; }

    public int TokensSaida { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal CustoEstimado { get; set; }

    public bool EncaminharSuporteHumano { get; set; }

    public static RespostaEstruturada De(RespostaAtendimento resposta) => new()
    {
        Resposta = resposta.Resposta,
        Categoria = resposta.Categoria,
        Fontes = resposta.Fontes,
        Modelo = resposta.Modelo,
        Fallback = resposta.Fallback,
        TokensEntrada = resposta.TokensEntrada,
        TokensSaida = resposta.TokensSaida,
        CustoEstimado = resposta.CustoEstimado,
        EncaminharSuporteHumano = resposta.EncaminharSuporteHumano
    };
}
