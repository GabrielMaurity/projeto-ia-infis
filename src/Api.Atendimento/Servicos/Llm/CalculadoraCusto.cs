namespace Api.Atendimento.Servicos.Llm;

/// <summary>
/// Custo de uma chamada ao modelo, em dólares (USD).
/// </summary>
public record CustoChamada(decimal Entrada, decimal Saida)
{
    public decimal Total => Entrada + Saida;
}

/// <summary>
/// Calcula o custo estimado de uma chamada a partir dos tokens e do preço por milhão de tokens.
/// </summary>
public static class CalculadoraCusto
{
    private const decimal MilhaoTokens = 1_000_000m;

    // Mesma escala da coluna DECIMAL(18, 8) do controle de custos.
    private const int CasasDecimais = 8;

    public static CustoChamada Calcular(
        int tokensEntrada,
        int tokensSaida,
        decimal custoEntradaPorMilhao,
        decimal custoSaidaPorMilhao)
        => new(
            Math.Round(tokensEntrada * custoEntradaPorMilhao / MilhaoTokens, CasasDecimais),
            Math.Round(tokensSaida * custoSaidaPorMilhao / MilhaoTokens, CasasDecimais));
}
