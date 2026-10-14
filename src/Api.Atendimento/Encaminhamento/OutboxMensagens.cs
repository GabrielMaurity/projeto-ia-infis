using System.Data;
using Microsoft.Data.SqlClient;

namespace Api.Atendimento.Encaminhamento;

/// <summary>
/// Mensagem de encaminhamento para o suporte humano, gravada na outbox.
/// Transmitido, data da transmissão e nome da fila ficam com os valores padrão da tabela
/// (false/null/null) até a mensagem ser publicada na fila.
/// </summary>
/// <param name="DataHoraGravacao">Data e hora (UTC) da gravação.</param>
/// <param name="Usuario">Usuário atendido.</param>
/// <param name="Descricao">Resumo do atendimento gerado pela IA.</param>
public record MensagemOutbox(DateTime DataHoraGravacao, string Usuario, string Descricao);

/// <summary>
/// Mensagem da outbox ainda não transmitida.
/// </summary>
public record MensagemOutboxPendente(long Id, string Usuario, string Descricao);

public interface IOutboxMensagens
{
    /// <summary>
    /// Grava a mensagem e retorna o Id gerado.
    /// </summary>
    Task<long> GravarAsync(MensagemOutbox mensagem, CancellationToken cancellationToken);

    /// <summary>
    /// Primeiras mensagens não transmitidas, na ordem de gravação.
    /// </summary>
    Task<IReadOnlyList<MensagemOutboxPendente>> ObterPendentesAsync(int quantidade, CancellationToken cancellationToken);

    /// <summary>
    /// Registra que a mensagem foi publicada na fila.
    /// </summary>
    Task MarcarTransmitidaAsync(long id, string nomeFila, DateTime dataHoraTransmissao, CancellationToken cancellationToken);
}

/// <summary>
/// Outbox na tabela dbo.OutboxMessage do SQL Server (bd-custos-ia).
/// </summary>
public sealed class OutboxMensagensSqlServer(string connectionString) : IOutboxMensagens
{
    private const string SqlInserir = """
        INSERT INTO dbo.OutboxMessage (DataHoraGravacao, Usuario, Descricao)
        OUTPUT INSERTED.Id
        VALUES (@DataHoraGravacao, @Usuario, @Descricao)
        """;

    private const string SqlPendentes = """
        SELECT TOP (@Quantidade) Id, Usuario, Descricao
        FROM dbo.OutboxMessage
        WHERE Transmitido = 0
        ORDER BY Id
        """;

    private const string SqlMarcarTransmitida = """
        UPDATE dbo.OutboxMessage
        SET Transmitido = 1, DataHoraTransmissao = @DataHoraTransmissao, NomeFila = @NomeFila
        WHERE Id = @Id AND Transmitido = 0
        """;

    public async Task<long> GravarAsync(MensagemOutbox mensagem, CancellationToken cancellationToken)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(SqlInserir, conexao);
        comando.Parameters.Add("@DataHoraGravacao", SqlDbType.DateTime2).Value = mensagem.DataHoraGravacao;
        comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 100).Value = mensagem.Usuario;
        comando.Parameters.Add("@Descricao", SqlDbType.NVarChar, -1).Value = mensagem.Descricao;

        return (long)(await comando.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task<IReadOnlyList<MensagemOutboxPendente>> ObterPendentesAsync(int quantidade, CancellationToken cancellationToken)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(SqlPendentes, conexao);
        comando.Parameters.Add("@Quantidade", SqlDbType.Int).Value = quantidade;

        var pendentes = new List<MensagemOutboxPendente>(quantidade);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);
        while (await leitor.ReadAsync(cancellationToken))
        {
            pendentes.Add(new MensagemOutboxPendente(leitor.GetInt64(0), leitor.GetString(1), leitor.GetString(2)));
        }

        return pendentes;
    }

    public async Task MarcarTransmitidaAsync(long id, string nomeFila, DateTime dataHoraTransmissao, CancellationToken cancellationToken)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(SqlMarcarTransmitida, conexao);
        comando.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
        comando.Parameters.Add("@NomeFila", SqlDbType.NVarChar, 200).Value = nomeFila;
        comando.Parameters.Add("@DataHoraTransmissao", SqlDbType.DateTime2).Value = dataHoraTransmissao;

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
