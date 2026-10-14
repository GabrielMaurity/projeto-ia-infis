using System.Data;
using Microsoft.Data.SqlClient;

namespace Worker.Suporte.Inbox;

/// <summary>
/// Valores da coluna InboxMessage.StatusEnvioEmail.
/// </summary>
public static class StatusEnvioEmail
{
    public const string Pendente = "Pendente";
    public const string Enviado = "Enviado";
    public const string Falha = "Falha";

    /// <summary>
    /// Reentrega de uma mensagem cujo e-mail já foi enviado: registrada, mas não reenviada.
    /// </summary>
    public const string Duplicada = "Duplicada";
}

/// <summary>
/// Uma leitura de mensagem da fila.
/// </summary>
public record RegistroInbox(
    string IdMensagemFila,
    DateTime DataHora,
    string NomeFila,
    string Usuario,
    string Descricao,
    string StatusEnvioEmail);

public interface IInboxMensagens
{
    /// <summary>
    /// Indica se o e-mail desta mensagem já foi enviado em uma leitura anterior.
    /// </summary>
    Task<bool> EmailJaEnviadoAsync(string idMensagemFila, CancellationToken cancellationToken);

    /// <summary>
    /// Grava a leitura e retorna o Id gerado.
    /// </summary>
    Task<long> RegistrarAsync(RegistroInbox registro, CancellationToken cancellationToken);

    Task AtualizarStatusAsync(long id, string status, CancellationToken cancellationToken);
}

/// <summary>
/// Histórico de leituras na tabela dbo.InboxMessage do SQL Server (bd-custos-ia).
/// </summary>
public sealed class InboxMensagensSqlServer(string connectionString) : IInboxMensagens
{
    private const string SqlJaEnviado = """
        SELECT CASE WHEN EXISTS (
            SELECT 1 FROM dbo.InboxMessage WHERE IdMensagemFila = @IdMensagemFila AND StatusEnvioEmail = N'Enviado'
        ) THEN 1 ELSE 0 END
        """;

    private const string SqlInserir = """
        INSERT INTO dbo.InboxMessage (IdMensagemFila, DataHora, NomeFila, Usuario, Descricao, StatusEnvioEmail)
        OUTPUT INSERTED.Id
        VALUES (@IdMensagemFila, @DataHora, @NomeFila, @Usuario, @Descricao, @StatusEnvioEmail)
        """;

    private const string SqlAtualizarStatus = "UPDATE dbo.InboxMessage SET StatusEnvioEmail = @StatusEnvioEmail WHERE Id = @Id";

    public async Task<bool> EmailJaEnviadoAsync(string idMensagemFila, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new SqlCommand(SqlJaEnviado, conexao);
        comando.Parameters.Add("@IdMensagemFila", SqlDbType.NVarChar, 100).Value = idMensagemFila;

        return (int)(await comando.ExecuteScalarAsync(cancellationToken))! == 1;
    }

    public async Task<long> RegistrarAsync(RegistroInbox registro, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new SqlCommand(SqlInserir, conexao);
        comando.Parameters.Add("@IdMensagemFila", SqlDbType.NVarChar, 100).Value = registro.IdMensagemFila;
        comando.Parameters.Add("@DataHora", SqlDbType.DateTime2).Value = registro.DataHora;
        comando.Parameters.Add("@NomeFila", SqlDbType.NVarChar, 200).Value = registro.NomeFila;
        comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 100).Value = registro.Usuario;
        comando.Parameters.Add("@Descricao", SqlDbType.NVarChar, -1).Value = registro.Descricao;
        comando.Parameters.Add("@StatusEnvioEmail", SqlDbType.NVarChar, 20).Value = registro.StatusEnvioEmail;

        return (long)(await comando.ExecuteScalarAsync(cancellationToken))!;
    }

    public async Task AtualizarStatusAsync(long id, string status, CancellationToken cancellationToken)
    {
        await using var conexao = await AbrirAsync(cancellationToken);
        await using var comando = new SqlCommand(SqlAtualizarStatus, conexao);
        comando.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
        comando.Parameters.Add("@StatusEnvioEmail", SqlDbType.NVarChar, 20).Value = status;

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqlConnection> AbrirAsync(CancellationToken cancellationToken)
    {
        var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(cancellationToken);
        return conexao;
    }
}
