using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Configuracoes;

/// <summary>
/// Configuração do Gemini, usado somente como modelo de contingência quando a OpenAI falha.
/// A chave deve vir de user-secrets ou da variável de ambiente Gemini__ApiKey, nunca do código.
/// </summary>
public class GeminiOptions
{
    public const string Secao = "Gemini";

    /// <summary>
    /// Chave da Gemini API (Google AI Studio).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Modelo usado no fallback.
    /// </summary>
    [Required]
    public string Modelo { get; set; } = "gemini-3.5-flash-lite";

    /// <summary>
    /// Preço, em USD, de 1 milhão de tokens de entrada do <see cref="Modelo"/>.
    /// </summary>
    [Range(typeof(decimal), "0", "1000")]
    public decimal CustoEntradaPorMilhaoTokens { get; set; } = 0.30m;

    /// <summary>
    /// Preço, em USD, de 1 milhão de tokens de saída do <see cref="Modelo"/> (inclui tokens de raciocínio).
    /// </summary>
    [Range(typeof(decimal), "0", "1000")]
    public decimal CustoSaidaPorMilhaoTokens { get; set; } = 2.50m;
}
