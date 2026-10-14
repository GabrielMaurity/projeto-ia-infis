using System.Data;
using Microsoft.Data.SqlClient;

namespace Api.Atendimento.Custos;

/// <summary>
/// Consumo de tokens de uma requisição ao atendimento. Custos em dólares (USD).
/// </summary>
/// <param name="Agente">Agente que atendeu.</param>
/// <param name="Modelo">Modelo que gerou a resposta, como informado pelo provedor.</param>
/// <param name="Usuario">Usuário atendido.</param>
/// <param name="DataHora">Data e hora (UTC) da resposta, a mesma gravada na conversa.</param>
/// <param name="TokensEntrada">Tokens de entrada consumidos.</param>
/// <param name="TokensSaida">Tokens de saída consumidos.</param>
/// <param name="CustoEntrada">Custo dos tokens de entrada.</param>
/// <param name="CustoSaida">Custo dos tokens de saída.</param>
/// <param name="IdConversa">Id (ObjectId) da conversa no MongoDB; nulo se ela não foi gravada.</param>
public record ConsumoTokens(
    string Agente,
    string Modelo,
    string Usuario,
    DateTime DataHora,
    int TokensEntrada,
    int TokensSaida,
    decimal CustoEntrada,
    decimal CustoSaida,
    string? IdConversa);

public interface IRegistroConsumoTokens
{
    Task RegistrarAsync(ConsumoTokens consumo, CancellationToken cancellationToken);
}

/// <summary>
/// Grava o consumo na tabela dbo.ConsumoTokens do SQL Server (bd-custos-ia).
/// O custo total é uma coluna calculada pelo banco.
/// </summary>
public sealed class RegistroConsumoTokensSqlServer(string connectionString) : IRegistroConsumoTokens
{
    private const string SqlInserir = """
        INSERT INTO dbo.ConsumoTokens
            (Agente, Modelo, Usuario, DataHora, TokensEntrada, TokensSaida, CustoEntrada, CustoSaida, IdConversa)
        VALUES
            (@Agente, @Modelo, @Usuario, @DataHora, @TokensEntrada, @TokensSaida, @CustoEntrada, @CustoSaida, @IdConversa)
        """;

    public async Task RegistrarAsync(ConsumoTokens consumo, CancellationToken cancellationToken)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(cancellationToken);

        await using var comando = new SqlCommand(SqlInserir, conexao);
        comando.Parameters.Add("@Agente", SqlDbType.NVarChar, 100).Value = consumo.Agente;
        comando.Parameters.Add("@Modelo", SqlDbType.NVarChar, 100).Value = consumo.Modelo;
        comando.Parameters.Add("@Usuario", SqlDbType.NVarChar, 100).Value = consumo.Usuario;
        comando.Parameters.Add("@DataHora", SqlDbType.DateTime2).Value = consumo.DataHora;
        comando.Parameters.Add("@TokensEntrada", SqlDbType.Int).Value = consumo.TokensEntrada;
        comando.Parameters.Add("@TokensSaida", SqlDbType.Int).Value = consumo.TokensSaida;
        AdicionarDecimal(comando, "@CustoEntrada", consumo.CustoEntrada);
        AdicionarDecimal(comando, "@CustoSaida", consumo.CustoSaida);
        comando.Parameters.Add("@IdConversa", SqlDbType.Char, 24).Value = (object?)consumo.IdConversa ?? DBNull.Value;

        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AdicionarDecimal(SqlCommand comando, string nome, decimal valor)
    {
        var parametro = comando.Parameters.Add(nome, SqlDbType.Decimal);
        parametro.Precision = 18;
        parametro.Scale = 8;
        parametro.Value = valor;
    }
}
