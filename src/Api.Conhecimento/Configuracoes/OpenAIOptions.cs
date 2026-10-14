using System.ComponentModel.DataAnnotations;

namespace Api.Conhecimento.Configuracoes;

/// <summary>
/// Configuração do acesso à OpenAI. A chave nunca fica no código nem no appsettings:
/// use user-secrets em desenvolvimento ou a variável de ambiente OpenAI__ApiKey.
/// </summary>
public class OpenAIOptions
{
    public const string Secao = "OpenAI";

    /// <summary>
    /// Chave da API da OpenAI.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Modelo usado para gerar os embeddings.
    /// </summary>
    [Required]
    public string ModeloEmbedding { get; set; } = "text-embedding-3-small";
}
