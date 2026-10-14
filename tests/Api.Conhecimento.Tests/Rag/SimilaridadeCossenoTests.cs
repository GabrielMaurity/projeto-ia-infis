using Api.Conhecimento.Rag;

namespace Api.Conhecimento.Tests.Rag;

public class SimilaridadeCossenoTests
{
    private const int Precisao = 6;

    [Fact]
    public void VetoresIguais_RetornaUm()
    {
        float[] vetor = [1f, 2f, 3f];

        var resultado = SimilaridadeCosseno.Calcular(vetor, vetor);

        Assert.Equal(1.0, resultado, Precisao);
    }

    [Fact]
    public void VetoresNaMesmaDirecaoComTamanhosDiferentes_RetornaUm()
    {
        var resultado = SimilaridadeCosseno.Calcular([1f, 2f, 3f], [2f, 4f, 6f]);

        Assert.Equal(1.0, resultado, Precisao);
    }

    [Fact]
    public void VetoresOrtogonais_RetornaZero()
    {
        var resultado = SimilaridadeCosseno.Calcular([1f, 0f], [0f, 1f]);

        Assert.Equal(0.0, resultado, Precisao);
    }

    [Fact]
    public void VetoresOpostos_RetornaMenosUm()
    {
        var resultado = SimilaridadeCosseno.Calcular([1f, 2f, 3f], [-1f, -2f, -3f]);

        Assert.Equal(-1.0, resultado, Precisao);
    }

    [Fact]
    public void VetoresQuaisquer_RetornaValorCalculadoPelaFormula()
    {
        // (1*4 + 2*5 + 3*6) / (sqrt(14) * sqrt(77)) = 32 / sqrt(1078)
        var esperado = 32 / Math.Sqrt(1078);

        var resultado = SimilaridadeCosseno.Calcular([1f, 2f, 3f], [4f, 5f, 6f]);

        Assert.Equal(esperado, resultado, Precisao);
    }

    [Fact]
    public void OrdemDosVetores_NaoAlteraResultado()
    {
        float[] a = [0.3f, -1.2f, 4f];
        float[] b = [2f, 0.5f, -0.7f];

        Assert.Equal(SimilaridadeCosseno.Calcular(a, b), SimilaridadeCosseno.Calcular(b, a), Precisao);
    }

    [Fact]
    public void VetorZerado_RetornaZero()
    {
        var resultado = SimilaridadeCosseno.Calcular([0f, 0f, 0f], [1f, 2f, 3f]);

        Assert.Equal(0.0, resultado);
    }

    [Fact]
    public void VetoresDeTamanhosDiferentes_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SimilaridadeCosseno.Calcular([1f, 2f], [1f, 2f, 3f]));
    }

    [Fact]
    public void VetoresVazios_LancaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SimilaridadeCosseno.Calcular([], []));
    }
}
