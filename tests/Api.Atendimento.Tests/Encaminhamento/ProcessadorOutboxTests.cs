using System.Text.Json;
using Api.Atendimento.Configuracoes;
using Api.Atendimento.Encaminhamento;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Api.Atendimento.Tests.Encaminhamento;

public class ProcessadorOutboxTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 13, 19, 0, 0, TimeSpan.Zero);

    private readonly OutboxFalsa _outbox = new();
    private readonly PublicadorFalso _publicador = new();

    [Fact]
    public async Task PublicarPendentesAsync_LeOLoteConfiguradoPublicaJsonEMarcaComoTransmitida()
    {
        _outbox.Pendentes = [new(7, "Maria", "Pediu atendimento humano."), new(8, "João", "Insatisfeito com a resposta.")];

        var transmitidas = await Processador().PublicarPendentesAsync(CancellationToken.None);

        Assert.Equal(2, transmitidas);
        Assert.Equal(5, _outbox.QuantidadeSolicitada);
        Assert.Equal(["7", "8"], _publicador.Publicadas.Select(p => p.IdMensagem));
        Assert.All(_publicador.Publicadas, p => Assert.Equal("atendimentos", p.Fila));
        Assert.Equal([(7L, "atendimentos", Agora.UtcDateTime), (8L, "atendimentos", Agora.UtcDateTime)], _outbox.Transmitidas);
    }

    [Fact]
    public async Task PublicarPendentesAsync_JsonComIdNomeDoUsuarioEDescricao()
    {
        _outbox.Pendentes = [new(7, "Maria", "Pediu atendimento humano.")];

        await Processador().PublicarPendentesAsync(CancellationToken.None);

        using var json = JsonDocument.Parse(_publicador.Publicadas[0].Corpo);
        Assert.Equal(7, json.RootElement.GetProperty("id").GetInt64());
        Assert.Equal("Maria", json.RootElement.GetProperty("nomeUsuario").GetString());
        Assert.Equal("Pediu atendimento humano.", json.RootElement.GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task PublicarPendentesAsync_FalhaNaPublicacao_NaoMarcaEInterrompeOLote()
    {
        _outbox.Pendentes = [new(7, "Maria", "A"), new(8, "João", "B"), new(9, "Ana", "C")];
        _publicador.FalharNoId = "8";

        var transmitidas = await Processador().PublicarPendentesAsync(CancellationToken.None);

        Assert.Equal(1, transmitidas);
        Assert.Equal([7L], _outbox.Transmitidas.Select(t => t.Id));
        Assert.DoesNotContain(_publicador.Publicadas, p => p.IdMensagem == "9");
    }

    [Fact]
    public async Task PublicarPendentesAsync_SemPendentes_NaoPublica()
    {
        var transmitidas = await Processador().PublicarPendentesAsync(CancellationToken.None);

        Assert.Equal(0, transmitidas);
        Assert.Empty(_publicador.Publicadas);
    }

    private ProcessadorOutbox Processador()
        => new(_outbox, _publicador, Options.Create(new OutboxOptions()), new RelogioFixo(Agora),
            NullLogger<ProcessadorOutbox>.Instance);

    private sealed class OutboxFalsa : IOutboxMensagens
    {
        public List<MensagemOutboxPendente> Pendentes { get; set; } = [];

        public int? QuantidadeSolicitada { get; private set; }

        public List<(long Id, string Fila, DateTime DataHora)> Transmitidas { get; } = [];

        public Task<long> GravarAsync(MensagemOutbox mensagem, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MensagemOutboxPendente>> ObterPendentesAsync(int quantidade, CancellationToken cancellationToken)
        {
            QuantidadeSolicitada = quantidade;
            return Task.FromResult<IReadOnlyList<MensagemOutboxPendente>>(Pendentes.Take(quantidade).ToList());
        }

        public Task MarcarTransmitidaAsync(long id, string nomeFila, DateTime dataHoraTransmissao, CancellationToken cancellationToken)
        {
            Transmitidas.Add((id, nomeFila, dataHoraTransmissao));
            return Task.CompletedTask;
        }
    }

    private sealed class PublicadorFalso : IPublicadorFila
    {
        public List<(string Fila, string IdMensagem, byte[] Corpo)> Publicadas { get; } = [];

        public string? FalharNoId { get; set; }

        public Task PublicarAsync(string fila, string idMensagem, ReadOnlyMemory<byte> corpoJson, CancellationToken cancellationToken)
        {
            if (idMensagem == FalharNoId)
            {
                throw new InvalidOperationException("RabbitMQ fora do ar");
            }

            Publicadas.Add((fila, idMensagem, corpoJson.ToArray()));
            return Task.CompletedTask;
        }
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
