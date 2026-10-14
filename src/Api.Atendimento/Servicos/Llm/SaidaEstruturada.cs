using System.Text.Json;

namespace Api.Atendimento.Servicos.Llm;

/// <summary>
/// Campos que o modelo preenche na resposta (Structured Output). O mesmo JSON Schema é usado
/// pela OpenAI e pelo Gemini, para que a resposta tenha o mesmo formato em qualquer provedor.
/// </summary>
public sealed record SaidaEstruturada(
    string Resposta,
    string Categoria,
    string[] Fontes,
    bool EncaminharSuporteHumano,
    string ResumoEncaminhamento)
{
    public const string NomeSchema = "resposta_atendimento";

    public const string JsonSchema = """
        {
          "type": "object",
          "properties": {
            "resposta": {
              "type": "string",
              "description": "Resposta ao usuário, baseada somente no contexto."
            },
            "categoria": {
              "type": "string",
              "enum": ["Tributária", "Contábil", "Sem informação", "Fora do escopo"],
              "description": "Assunto da pergunta e se o contexto respondeu."
            },
            "fontes": {
              "type": "array",
              "items": { "type": "string" },
              "description": "Nomes dos documentos do contexto (campo Fonte) usados na resposta. Lista vazia se nenhum foi usado."
            },
            "encaminharSuporteHumano": {
              "type": "boolean",
              "description": "true se o usuário pediu atendimento humano ou demonstrou insatisfação com o atendimento."
            },
            "resumoEncaminhamento": {
              "type": "string",
              "description": "Resumo do atendimento para o atendente humano quando encaminharSuporteHumano for true; vazio caso contrário."
            }
          },
          "required": ["resposta", "categoria", "fontes", "encaminharSuporteHumano", "resumoEncaminhamento"],
          "additionalProperties": false
        }
        """;

    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Converte o JSON devolvido pelo modelo.
    /// </summary>
    /// <param name="json">Texto da resposta do modelo.</param>
    /// <param name="provedor">Nome do provedor, usado nas mensagens de erro.</param>
    public static SaidaEstruturada Ler(string? json, string provedor)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException($"{provedor} retornou uma resposta vazia.");
        }

        var saida = JsonSerializer.Deserialize<SaidaEstruturada>(json, OpcoesJson);

        if (saida is null || string.IsNullOrWhiteSpace(saida.Resposta) || string.IsNullOrWhiteSpace(saida.Categoria))
        {
            throw new InvalidOperationException($"Não foi possível interpretar a resposta estruturada de {provedor}.");
        }

        return saida with
        {
            Fontes = saida.Fontes ?? [],
            ResumoEncaminhamento = saida.ResumoEncaminhamento?.Trim() ?? string.Empty
        };
    }
}
