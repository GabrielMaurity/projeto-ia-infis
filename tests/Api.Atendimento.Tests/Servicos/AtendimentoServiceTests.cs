using Api.Atendimento.Configuracoes;
using Api.Atendimento.Custos;
using Api.Atendimento.Encaminhamento;
using Api.Atendimento.Historico;
using Api.Atendimento.Modelos;
using Api.Atendimento.Servicos;
using Api.Atendimento.Servicos.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Api.Atendimento.Tests.Servicos;

public class AtendimentoServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 8, 15, 30, 0, TimeSpan.Zero);

    private readonly ConhecimentoMcpFalso _mcp = new();
    private readonly LlmFalso _llm = new();
    private readonly HistoricoFalso _historico = new();
    private readonly RegistroConsumoFalso _consumo = new();
    private readonly OutboxFalsa _outbox = new();
    private readonly AtendimentoOptions _opcoes = new() { Agente = "agente-teste", QuantidadeConversasHistorico = 3 };

    [Fact]
    public async Task ResponderAsync_BuscaContextoViaMcpEEnviaInstrucoesEMensagemAoModelo()
    {
        _mcp.Contexto = "[Trecho 1] Fonte: reforma.pdf | Similaridade: 0.8\nA CBS substitui PIS e COFINS.";

        await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.Equal("O que é a CBS?", _mcp.PerguntaRecebida);
        Assert.Equal(PromptAtendimento.Instrucoes, _llm.InstrucoesRecebidas);
        Assert.Contains("<usuario>Maria</usuario>", _llm.MensagemRecebida);
        Assert.Contains("A CBS substitui PIS e COFINS.", _llm.MensagemRecebida);
        Assert.Contains("O que é a CBS?", _llm.MensagemRecebida);
    }

    [Fact]
    public async Task ResponderAsync_RetornaRespostaEstruturadaComTokensECusto()
    {
        var resposta = await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.Equal("Resposta", resposta.Resposta);
        Assert.Equal("Tributária", resposta.Categoria);
        Assert.Equal(["reforma.pdf"], resposta.Fontes);
        Assert.Equal("gpt-4.1-mini", resposta.Modelo);
        Assert.False(resposta.Fallback);
        Assert.Equal(1200, resposta.TokensEntrada);
        Assert.Equal(300, resposta.TokensSaida);
        Assert.Equal(0.00096m, resposta.CustoEstimado);
    }

    [Fact]
    public async Task ResponderAsync_BuscaAsUltimasConversasDoUsuarioEAsEnviaNoPrompt()
    {
        _historico.Conversas =
        [
            Conversa("O que é a CBS?", "A CBS é a contribuição federal sobre bens e serviços."),
            Conversa("Qual a alíquota de teste?", "Em 2026 a alíquota de teste da CBS é 0,9%."),
        ];

        await Servico().ResponderAsync("Maria", "E a do IBS?", CancellationToken.None);

        Assert.Equal(("Maria", "agente-teste", 3), _historico.ConsultaRecebida);
        Assert.Contains("Pergunta: O que é a CBS?", _llm.MensagemRecebida);
        Assert.Contains("Resposta: Em 2026 a alíquota de teste da CBS é 0,9%.", _llm.MensagemRecebida);
        Assert.DoesNotContain(PromptAtendimento.SemHistorico, _llm.MensagemRecebida);
    }

    [Fact]
    public async Task ResponderAsync_SemConversasAnteriores_InformaQueNaoHaHistorico()
    {
        await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.Contains($"<historico>\n{PromptAtendimento.SemHistorico}\n</historico>", _llm.MensagemRecebida!.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task ResponderAsync_HistoricoDesligado_NaoConsultaOMongo()
    {
        _opcoes.QuantidadeConversasHistorico = 0;

        await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.Null(_historico.ConsultaRecebida);
    }

    [Fact]
    public async Task ResponderAsync_GravaAConversaComRespostaEstruturadaETexto()
    {
        await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        var gravada = Assert.Single(_historico.Gravadas);
        Assert.Equal(Agora.UtcDateTime, gravada.DataHora);
        Assert.Equal("Maria", gravada.Usuario);
        Assert.Equal("agente-teste", gravada.Agente);
        Assert.Equal("O que é a CBS?", gravada.Pergunta);
        Assert.Equal("Resposta", gravada.RespostaTexto);
        Assert.Equal("Tributária", gravada.RespostaEstruturada.Categoria);
        Assert.Equal(["reforma.pdf"], gravada.RespostaEstruturada.Fontes);
        Assert.Equal(1200, gravada.RespostaEstruturada.TokensEntrada);
        Assert.Equal(0.00096m, gravada.RespostaEstruturada.CustoEstimado);
    }

    [Fact]
    public async Task ResponderAsync_FalhaAoLerHistorico_RespondeMesmoAssim()
    {
        _historico.ErroLeitura = new TimeoutException("MongoDB fora do ar");

        var resposta = await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.Equal("Resposta", resposta.Resposta);
        Assert.Contains(PromptAtendimento.SemHistorico, _llm.MensagemRecebida);
    }

    [Fact]
    public async Task ResponderAsync_FalhaAoGravarHistorico_DevolveARespostaMesmoAssim()
    {
        _historico.ErroGravacao = new TimeoutException("MongoDB fora do ar");

        var resposta = await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.Equal("Resposta", resposta.Resposta);
    }

    [Fact]
    public async Task ResponderAsync_FalhaNoMcp_PropagaSemChamarOModeloNemGravar()
    {
        _mcp.Erro = new HttpRequestException("Api.Conhecimento fora do ar");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None));

        Assert.Null(_llm.MensagemRecebida);
        Assert.Empty(_historico.Gravadas);
        Assert.Empty(_consumo.Registrados);
    }

    [Fact]
    public async Task ResponderAsync_RespostaDoModeloDeContingencia_RetornaEGravaFallback()
    {
        _llm.Fallback = true;

        var resposta = await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.True(resposta.Fallback);
        Assert.True(Assert.Single(_historico.Gravadas).RespostaEstruturada.Fallback);
    }

    [Fact]
    public async Task ResponderAsync_RegistraConsumoComCustosSeparadosEIdDaConversa()
    {
        await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        var consumo = Assert.Single(_consumo.Registrados);
        Assert.Equal(
            new ConsumoTokens("agente-teste", "gpt-4.1-mini", "Maria", Agora.UtcDateTime, 1200, 300, 0.00048m, 0.00048m, HistoricoFalso.IdGerado),
            consumo);
        Assert.Equal(consumo.DataHora, Assert.Single(_historico.Gravadas).DataHora);
    }

    [Fact]
    public async Task ResponderAsync_FalhaAoGravarHistorico_RegistraConsumoSemIdDaConversa()
    {
        _historico.ErroGravacao = new TimeoutException("MongoDB fora do ar");

        await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.Null(Assert.Single(_consumo.Registrados).IdConversa);
    }

    [Fact]
    public async Task ResponderAsync_FalhaAoRegistrarConsumo_DevolveARespostaMesmoAssim()
    {
        _consumo.Erro = new InvalidOperationException("SQL Server fora do ar");

        var resposta = await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.Equal("Resposta", resposta.Resposta);
        Assert.Single(_historico.Gravadas);
    }

    [Fact]
    public async Task ResponderAsync_ModeloIndicouEncaminhamento_ExecutaAFuncaoDoKernelEGravaNaOutbox()
    {
        _llm.EncaminharSuporteHumano = true;
        _llm.ResumoEncaminhamento = "Usuária pediu para falar com um atendente sobre a CBS.";

        var resposta = await Servico().ResponderAsync("Maria", "Quero falar com um atendente.", CancellationToken.None);

        Assert.True(resposta.EncaminharSuporteHumano);
        Assert.True(Assert.Single(_historico.Gravadas).RespostaEstruturada.EncaminharSuporteHumano);
        Assert.Equal(
            new MensagemOutbox(Agora.UtcDateTime, "Maria", "Usuária pediu para falar com um atendente sobre a CBS."),
            Assert.Single(_outbox.Gravadas));
    }

    [Fact]
    public async Task ResponderAsync_SemEncaminhamento_NaoGravaNaOutbox()
    {
        var resposta = await Servico().ResponderAsync("Maria", "O que é a CBS?", CancellationToken.None);

        Assert.False(resposta.EncaminharSuporteHumano);
        Assert.Empty(_outbox.Gravadas);
    }

    [Fact]
    public async Task ResponderAsync_EncaminhamentoSemResumo_GravaPerguntaERespostaComoDescricao()
    {
        _llm.EncaminharSuporteHumano = true;

        await Servico().ResponderAsync("Maria", "Isso não me ajudou em nada!", CancellationToken.None);

        var descricao = Assert.Single(_outbox.Gravadas).Descricao;
        Assert.Contains("Isso não me ajudou em nada!", descricao);
        Assert.Contains("Resposta", descricao);
    }

    [Fact]
    public async Task ResponderAsync_FalhaAoGravarNaOutbox_DevolveARespostaMesmoAssim()
    {
        _llm.EncaminharSuporteHumano = true;
        _llm.ResumoEncaminhamento = "Resumo";
        _outbox.Erro = new InvalidOperationException("SQL Server fora do ar");

        var resposta = await Servico().ResponderAsync("Maria", "Quero falar com um humano.", CancellationToken.None);

        Assert.True(resposta.EncaminharSuporteHumano);
        Assert.Single(_consumo.Registrados);
    }

    private AtendimentoService Servico()
        => new(_mcp, _llm, _historico, _consumo, CriarKernel(), Options.Create(_opcoes), new RelogioFixo(Agora), NullLogger<AtendimentoService>.Instance);

    /// <summary>
    /// Kernel real com o plugin de encaminhamento; só a outbox é falsa.
    /// </summary>
    private Kernel CriarKernel()
    {
        var kernel = new Kernel();
        kernel.Plugins.AddFromObject(
            new EncaminhamentoPlugin(_outbox, new RelogioFixo(Agora), NullLogger<EncaminhamentoPlugin>.Instance),
            EncaminhamentoPlugin.NomePlugin);
        return kernel;
    }

    private static ConversaAtendimento Conversa(string pergunta, string resposta)
        => new() { DataHora = Agora.UtcDateTime.AddHours(-1), Usuario = "Maria", Pergunta = pergunta, RespostaTexto = resposta };

    private sealed class ConhecimentoMcpFalso : IConhecimentoMcpClient
    {
        public string Contexto { get; set; } = "contexto";

        public Exception? Erro { get; set; }

        public string? PerguntaRecebida { get; private set; }

        public Task<IReadOnlyList<FerramentaMcp>> ListarFerramentasAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<string> BuscarConhecimentoAsync(string pergunta, CancellationToken cancellationToken)
        {
            PerguntaRecebida = pergunta;
            return Erro is null ? Task.FromResult(Contexto) : throw Erro;
        }
    }

    private sealed class LlmFalso : ILlmClient
    {
        public string? InstrucoesRecebidas { get; private set; }

        public string? MensagemRecebida { get; private set; }

        public bool Fallback { get; set; }

        public bool EncaminharSuporteHumano { get; set; }

        public string ResumoEncaminhamento { get; set; } = string.Empty;

        public Task<RespostaLlm> GerarRespostaAsync(string instrucoes, string mensagem, CancellationToken cancellationToken)
        {
            InstrucoesRecebidas = instrucoes;
            MensagemRecebida = mensagem;
            return Task.FromResult(new RespostaLlm(
                "Resposta", "Tributária", ["reforma.pdf"], "gpt-4.1-mini", 1200, 300, new CustoChamada(0.00048m, 0.00048m),
                EncaminharSuporteHumano: EncaminharSuporteHumano,
                ResumoEncaminhamento: ResumoEncaminhamento,
                Fallback: Fallback));
        }
    }

    private sealed class OutboxFalsa : IOutboxMensagens
    {
        public List<MensagemOutbox> Gravadas { get; } = [];

        public Exception? Erro { get; set; }

        public Task<long> GravarAsync(MensagemOutbox mensagem, CancellationToken cancellationToken)
        {
            if (Erro is not null)
            {
                throw Erro;
            }

            Gravadas.Add(mensagem);
            return Task.FromResult((long)Gravadas.Count);
        }

        public Task<IReadOnlyList<MensagemOutboxPendente>> ObterPendentesAsync(int quantidade, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task MarcarTransmitidaAsync(long id, string nomeFila, DateTime dataHoraTransmissao, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class RegistroConsumoFalso : IRegistroConsumoTokens
    {
        public List<ConsumoTokens> Registrados { get; } = [];

        public Exception? Erro { get; set; }

        public Task RegistrarAsync(ConsumoTokens consumo, CancellationToken cancellationToken)
        {
            if (Erro is not null)
            {
                throw Erro;
            }

            Registrados.Add(consumo);
            return Task.CompletedTask;
        }
    }

    private sealed class HistoricoFalso : IHistoricoAtendimentos
    {
        public const string IdGerado = "6ac7bc6ffd72c41f794883ab";

        public List<ConversaAtendimento> Conversas { get; set; } = [];

        public List<ConversaAtendimento> Gravadas { get; } = [];

        public (string Usuario, string Agente, int Quantidade)? ConsultaRecebida { get; private set; }

        public Exception? ErroLeitura { get; set; }

        public Exception? ErroGravacao { get; set; }

        public Task GravarAsync(ConversaAtendimento conversa, CancellationToken cancellationToken)
        {
            if (ErroGravacao is not null)
            {
                throw ErroGravacao;
            }

            // Como o driver do MongoDB, preenche o Id ao inserir.
            conversa.Id = IdGerado;
            Gravadas.Add(conversa);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ConversaAtendimento>> ObterUltimasAsync(
            string usuario, string agente, int quantidade, CancellationToken cancellationToken)
        {
            ConsultaRecebida = (usuario, agente, quantidade);
            return ErroLeitura is null
                ? Task.FromResult<IReadOnlyList<ConversaAtendimento>>(Conversas)
                : throw ErroLeitura;
        }
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
