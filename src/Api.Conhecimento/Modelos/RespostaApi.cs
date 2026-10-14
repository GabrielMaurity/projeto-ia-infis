namespace Api.Conhecimento.Modelos;

/// <summary>
/// Resposta padrão da API.
/// </summary>
/// <param name="Status">Status HTTP.</param>
/// <param name="Mensagem">Mensagem para o cliente.</param>
public record RespostaApi(int Status, string Mensagem);
