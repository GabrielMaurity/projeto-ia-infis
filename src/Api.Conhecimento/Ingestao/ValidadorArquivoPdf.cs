namespace Api.Conhecimento.Ingestao;

/// <summary>
/// Confere se o arquivo enviado é um PDF dentro do tamanho permitido.
/// </summary>
public static class ValidadorArquivoPdf
{
    private static ReadOnlySpan<byte> AssinaturaPdf => "%PDF-"u8;

    /// <summary>
    /// Retorna a mensagem de erro ou <c>null</c> quando o arquivo é válido.
    /// A extensão não basta: o conteúdo precisa começar com a assinatura "%PDF-".
    /// </summary>
    public static string? Validar(string nomeArquivo, ReadOnlySpan<byte> conteudo, long tamanhoMaximoBytes)
    {
        if (conteudo.IsEmpty)
        {
            return "O arquivo enviado está vazio.";
        }

        if (conteudo.Length > tamanhoMaximoBytes)
        {
            return $"O arquivo excede o tamanho máximo de {FormatarTamanho(tamanhoMaximoBytes)}.";
        }

        if (!string.Equals(Path.GetExtension(nomeArquivo), ".pdf", StringComparison.OrdinalIgnoreCase)
            || !conteudo.StartsWith(AssinaturaPdf))
        {
            return "O arquivo enviado não é um PDF.";
        }

        return null;
    }

    private static string FormatarTamanho(long bytes)
        => bytes % (1024 * 1024) == 0 ? $"{bytes / (1024 * 1024)} MB" : $"{bytes / 1024.0:0.#} KB";
}
