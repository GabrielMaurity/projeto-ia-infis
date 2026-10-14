using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Worker.Suporte.Email;
using Worker.Suporte.Inbox;
using Worker.Suporte.Processamento;

namespace Worker.Suporte.Tests.Processamento;

public class ProcessadorMensagemSuporteTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 13, 19, 30, 0, TimeSpan.Zero);

    private static readonly byte[] Corpo =
        Encoding.UTF8.GetBytes("""{"id":7,"nomeUsuario":"Maria","descricao":"Pediu atendimento humano."}""");

    private readonly InboxFalsa _inbox = new();
    private readonly EnviadorFalso _enviador = new();

    [Fact]
    public async Task ProcessarAsync_MensagemNova_RegistraNaInboxEnviaEmailEMarcaEnviado()
    {
        var resultado = await Processador().ProcessarAsync("7", Corpo, "atendimentos", CancellationToken.None);

        Assert.Equal(ResultadoProcessamento.Concluido, resultado);
        Assert.Equal(
            new RegistroInbox("7", Agora.UtcDateTime, "atendimentos", "Maria", "Pediu atendimento humano.", StatusEnvioEmail.Pendente),
            Assert.Single(_inbox.Registros));
        Assert.Equal("Novo atendimento solicitado por: Maria", Assert.Single(_enviador.Enviados).Assunto);
        Assert.Equal([(1L, StatusEnvioEmail.Enviado)], _inbox.Atualizacoes);
    }

    [Fact]
    public async Task ProcessarAsync_EmailJaEnviado_RegistraComoDuplicadaSemReenviar()
    {
        _inbox.JaEnviados.Add("7");

        var resultado = await Processador().ProcessarAsync("7", Corpo, "atendimentos", CancellationToken.None);

        Assert.Equal(ResultadoProcessamento.Concluido, resultado);
        Assert.Equal(StatusEnvioEmail.Duplicada, Assert.Single(_inbox.Registros).StatusEnvioEmail);
        Assert.Empty(_enviador.Enviados);
    }

    [Fact]
    public async Task ProcessarAsync_FalhaNoEnvio_MarcaFalhaEPedeReprocessamento()
    {
        _enviador.Erro = new InvalidOperationException("SMTP fora do ar");

        var resultado = await Processador().ProcessarAsync("7", Corpo, "atendimentos", CancellationToken.None);

        Assert.Equal(ResultadoProcessamento.Reprocessar, resultado);
        Assert.Equal([(1L, StatusEnvioEmail.Falha)], _inbox.Atualizacoes);
    }

    [Fact]
    public async Task ProcessarAsync_JsonInvalido_DescartaSemRegistrar()
    {
        var resultado = await Processador().ProcessarAsync(
            "7", Encoding.UTF8.GetBytes("lixo"), "atendimentos", CancellationToken.None);

        Assert.Equal(ResultadoProcessamento.Descartar, resultado);
        Assert.Empty(_inbox.Registros);
        Assert.Empty(_enviador.Enviados);
    }

    [Fact]
    public async Task ProcessarAsync_SemMessageId_UsaOIdDoJson()
    {
        await Processador().ProcessarAsync(null, Corpo, "atendimentos", CancellationToken.None);

        Assert.Equal("7", Assert.Single(_inbox.Registros).IdMensagemFila);
    }

    private ProcessadorMensagemSuporte Processador()
        => new(_inbox, _enviador, new RelogioFixo(Agora), NullLogger<ProcessadorMensagemSuporte>.Instance);

    private sealed class InboxFalsa : IInboxMensagens
    {
        public HashSet<string> JaEnviados { get; } = [];

        public List<RegistroInbox> Registros { get; } = [];

        public List<(long Id, string Status)> Atualizacoes { get; } = [];

        public Task<bool> EmailJaEnviadoAsync(string idMensagemFila, CancellationToken cancellationToken)
            => Task.FromResult(JaEnviados.Contains(idMensagemFila));

        public Task<long> RegistrarAsync(RegistroInbox registro, CancellationToken cancellationToken)
        {
            Registros.Add(registro);
            return Task.FromResult((long)Registros.Count);
        }

        public Task AtualizarStatusAsync(long id, string status, CancellationToken cancellationToken)
        {
            Atualizacoes.Add((id, status));
            return Task.CompletedTask;
        }
    }

    private sealed class EnviadorFalso : IEnviadorEmail
    {
        public List<EmailSuporte> Enviados { get; } = [];

        public Exception? Erro { get; set; }

        public Task EnviarAsync(EmailSuporte email, CancellationToken cancellationToken)
        {
            if (Erro is not null)
            {
                throw Erro;
            }

            Enviados.Add(email);
            return Task.CompletedTask;
        }
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
