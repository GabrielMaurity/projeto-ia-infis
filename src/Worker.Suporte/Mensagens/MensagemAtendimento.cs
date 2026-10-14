using System.Text.Json;

namespace Worker.Suporte.Mensagens;

/// <summary>
/// JSON publicado pela Api.Atendimento na fila "atendimentos".
/// </summary>
/// <param name="Id">Id da OutboxMessage de origem.</param>
/// <param name="NomeUsuario">Usuário atendido.</param>
/// <param name="Descricao">Resumo do atendimento gerado pela IA.</param>
public record MensagemAtendimento(long Id, string NomeUsuario, string Descricao)
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Converte o corpo da mensagem; retorna <c>null</c> se o JSON for inválido ou incompleto.
    /// </summary>
    public static MensagemAtendimento? Ler(ReadOnlySpan<byte> corpo)
    {
        try
        {
            var mensagem = JsonSerializer.Deserialize<MensagemAtendimento>(corpo, OpcoesJson);

            return mensagem is null
                || string.IsNullOrWhiteSpace(mensagem.NomeUsuario)
                || string.IsNullOrWhiteSpace(mensagem.Descricao)
                ? null
                : mensagem;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
