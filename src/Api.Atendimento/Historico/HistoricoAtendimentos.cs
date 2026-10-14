using Api.Atendimento.Configuracoes;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Api.Atendimento.Historico;

public interface IHistoricoAtendimentos
{
    Task GravarAsync(ConversaAtendimento conversa, CancellationToken cancellationToken);

    /// <summary>
    /// Últimas conversas do usuário com o agente, em ordem cronológica (mais antiga primeiro).
    /// </summary>
    Task<IReadOnlyList<ConversaAtendimento>> ObterUltimasAsync(
        string usuario,
        string agente,
        int quantidade,
        CancellationToken cancellationToken);
}

/// <summary>
/// Histórico de atendimentos na collection do MongoDB.
/// </summary>
public sealed class HistoricoAtendimentosMongo : IHistoricoAtendimentos
{
    private readonly IMongoCollection<ConversaAtendimento> _colecao;

    static HistoricoAtendimentosMongo()
    {
        // Campos em camelCase no MongoDB (dataHora, respostaEstruturada...), só para os tipos do histórico.
        ConventionRegistry.Register(
            "HistoricoAtendimentos",
            new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) },
            tipo => tipo.Namespace == typeof(ConversaAtendimento).Namespace);
    }

    public HistoricoAtendimentosMongo(IMongoClient client, IOptions<HistoricoMongoOptions> options)
    {
        var config = options.Value;
        _colecao = client.GetDatabase(config.Banco).GetCollection<ConversaAtendimento>(config.Colecao);
    }

    public Task GravarAsync(ConversaAtendimento conversa, CancellationToken cancellationToken)
        => _colecao.InsertOneAsync(conversa, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<ConversaAtendimento>> ObterUltimasAsync(
        string usuario,
        string agente,
        int quantidade,
        CancellationToken cancellationToken)
    {
        var recentes = await _colecao
            .Find(c => c.Usuario == usuario && c.Agente == agente)
            .SortByDescending(c => c.DataHora)
            .Limit(quantidade)
            .ToListAsync(cancellationToken);

        recentes.Reverse();
        return recentes;
    }
}
