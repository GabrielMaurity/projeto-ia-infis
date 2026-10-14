using Api.Atendimento.Historico;
using Api.Atendimento.Servicos.Llm;

namespace Api.Atendimento.Tests.Servicos;

public class PromptAtendimentoTests
{
    [Fact]
    public void MontarMensagem_FormataOHistoricoEmOrdemComDataPerguntaEResposta()
    {
        ConversaAtendimento[] historico =
        [
            new() { DataHora = new DateTime(2026, 10, 8, 14, 5, 0, DateTimeKind.Utc), Pergunta = "O que é a CBS?", RespostaTexto = "É a contribuição federal." },
            new() { DataHora = new DateTime(2026, 10, 8, 14, 30, 0, DateTimeKind.Utc), Pergunta = "E o IBS?", RespostaTexto = "É o imposto subnacional." },
        ];

        var mensagem = PromptAtendimento.MontarMensagem("Maria", historico, "contexto", "E as alíquotas?").Replace("\r\n", "\n");

        Assert.Contains(
            """
            <historico>
            [Conversa 1 - 08/10/2026 14:05 UTC]
            Pergunta: O que é a CBS?
            Resposta: É a contribuição federal.

            [Conversa 2 - 08/10/2026 14:30 UTC]
            Pergunta: E o IBS?
            Resposta: É o imposto subnacional.
            </historico>
            """.Replace("\r\n", "\n"),
            mensagem);
    }

    [Fact]
    public void MontarMensagem_PreencheUsuarioContextoEPerguntaNasTags()
    {
        var mensagem = PromptAtendimento.MontarMensagem("Maria", [], "[Trecho 1] Fonte: reforma.pdf", "O que é a CBS?");

        Assert.Contains("<usuario>Maria</usuario>", mensagem);
        Assert.Contains("<contexto>\n[Trecho 1] Fonte: reforma.pdf\n</contexto>", mensagem.Replace("\r\n", "\n"));
        Assert.Contains("<pergunta>\nO que é a CBS?\n</pergunta>", mensagem.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MontarMensagem_TextoComMarcadoresNaoESubstituidoDeNovo()
    {
        ConversaAtendimento[] historico = [new() { Pergunta = "histórico com {pergunta}", RespostaTexto = "ok" }];

        var mensagem = PromptAtendimento.MontarMensagem("{pergunta}", historico, "contexto com {usuario}", "pergunta com {contexto}");

        Assert.Contains("<usuario>{pergunta}</usuario>", mensagem);
        Assert.Contains("histórico com {pergunta}", mensagem);
        Assert.Contains("contexto com {usuario}", mensagem);
        Assert.Contains("pergunta com {contexto}", mensagem);
    }

    [Fact]
    public void Instrucoes_DefinemAsDuasCondicoesDeEncaminhamentoParaSuporteHumano()
    {
        Assert.Contains("## Encaminhamento para suporte humano", PromptAtendimento.Instrucoes);
        Assert.Contains("pede explicitamente para falar com uma pessoa", PromptAtendimento.Instrucoes);
        Assert.Contains("demonstra insatisfação com o atendimento", PromptAtendimento.Instrucoes);
        Assert.Contains("resumoEncaminhamento", PromptAtendimento.Instrucoes);
    }

    [Fact]
    public void Instrucoes_ListamTodasAsCategoriasPermitidas()
    {
        Assert.All(PromptAtendimento.Categorias, c => Assert.Contains($"\"{c}\"", PromptAtendimento.Instrucoes));
    }
}
