using Api.Atendimento.Servicos.Llm;

namespace Api.Atendimento.Tests.Servicos;

public class CalculadoraCustoTests
{
    [Fact]
    public void Calcular_SeparaEntradaESaidaPeloPrecoPorMilhao()
    {
        // 1.000 tokens a US$ 0,40/M = 0,0004; 500 tokens a US$ 1,60/M = 0,0008
        var custo = CalculadoraCusto.Calcular(1_000, 500, 0.40m, 1.60m);

        Assert.Equal(0.0004m, custo.Entrada);
        Assert.Equal(0.0008m, custo.Saida);
        Assert.Equal(0.0012m, custo.Total);
    }

    [Fact]
    public void Calcular_ArredondaCadaParcelaEm8CasasDecimais()
    {
        // 1 token a US$ 0,15/M = 0,00000015 -> 0,00000015 (8 casas); 1 token a US$ 0,333/M = 0,000000333 -> 0,00000033
        var custo = CalculadoraCusto.Calcular(1, 1, 0.15m, 0.333m);

        Assert.Equal(0.00000015m, custo.Entrada);
        Assert.Equal(0.00000033m, custo.Saida);
    }

    [Fact]
    public void Calcular_SemTokens_RetornaZero()
    {
        Assert.Equal(new CustoChamada(0m, 0m), CalculadoraCusto.Calcular(0, 0, 0.40m, 1.60m));
    }
}
