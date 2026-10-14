using Api.Atendimento.Encaminhamento;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;

namespace Api.Atendimento.Tests.Encaminhamento;

public class EncaminhamentoPluginTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 13, 18, 0, 0, TimeSpan.Zero);

    private readonly OutboxFalsa _outbox = new();

    [Fact]
    public void Plugin_ExpoeAFuncaoDeEncaminhamentoComDescricaoEParametros()
    {
        var funcao = Kernel().Plugins.GetFunction(EncaminhamentoPlugin.NomePlugin, EncaminhamentoPlugin.NomeFuncao);

        Assert.False(string.IsNullOrWhiteSpace(funcao.Description));
        Assert.Equal(["usuario", "descricao"], funcao.Metadata.Parameters.Select(p => p.Name));
        Assert.All(funcao.Metadata.Parameters, p => Assert.True(p.IsRequired));
    }

    [Fact]
    public async Task InvokeAsync_GravaNaOutboxComUsuarioDescricaoEDataERetornaOId()
    {
        var resultado = await Kernel().InvokeAsync(
            EncaminhamentoPlugin.NomePlugin,
            EncaminhamentoPlugin.NomeFuncao,
            new KernelArguments { ["usuario"] = "Maria", ["descricao"] = "Resumo do atendimento." });

        Assert.Equal(42L, resultado.GetValue<long>());
        Assert.Equal(new MensagemOutbox(Agora.UtcDateTime, "Maria", "Resumo do atendimento."), _outbox.Gravada);
    }

    private Kernel Kernel()
    {
        var kernel = new Kernel();
        kernel.Plugins.AddFromObject(
            new EncaminhamentoPlugin(_outbox, new RelogioFixo(Agora), NullLogger<EncaminhamentoPlugin>.Instance),
            EncaminhamentoPlugin.NomePlugin);
        return kernel;
    }

    private sealed class OutboxFalsa : IOutboxMensagens
    {
        public MensagemOutbox? Gravada { get; private set; }

        public Task<long> GravarAsync(MensagemOutbox mensagem, CancellationToken cancellationToken)
        {
            Gravada = mensagem;
            return Task.FromResult(42L);
        }

        public Task<IReadOnlyList<MensagemOutboxPendente>> ObterPendentesAsync(int quantidade, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task MarcarTransmitidaAsync(long id, string nomeFila, DateTime dataHoraTransmissao, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
