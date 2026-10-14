using System.ComponentModel.DataAnnotations;

namespace Api.Atendimento.Configuracoes;

/// <summary>
/// Configuração do acesso à OpenAI. A chave deve vir de user-secrets ou da
/// variável de ambiente OpenAI__ApiKey, nunca do código.
/// </summary>
public class OpenAIOptions
{
    public const string Secao = "OpenAI";

    /// <summary>
    /// Chave da API da OpenAI.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Modelo de chat usado para gerar as respostas.
    /// </summary>
    [Required]
    public string ModeloChat { get; set; } = "gpt-4.1-mini";

    /// <summary>
    /// Preço, em USD, de 1 milhão de tokens de entrada do <see cref="ModeloChat"/>.
    /// </summary>
    [Range(typeof(decimal), "0", "1000")]
    public decimal CustoEntradaPorMilhaoTokens { get; set; } = 0.40m;

    /// <summary>
    /// Preço, em USD, de 1 milhão de tokens de saída do <see cref="ModeloChat"/>.
    /// </summary>
    [Range(typeof(decimal), "0", "1000")]
    public decimal CustoSaidaPorMilhaoTokens { get; set; } = 1.60m;

    /// <summary>
    /// Tempo máximo de cada tentativa de chamada à OpenAI. Ao estourar, o Gemini assume (fallback).
    /// </summary>
    [Range(5, 300)]
    public int TimeoutSegundos { get; set; } = 30;
}
