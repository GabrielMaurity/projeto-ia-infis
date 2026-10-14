using Api.Conhecimento.Rag;
using Npgsql;
using Pgvector;

namespace Api.Conhecimento.Ingestao;

/// <summary>
/// Dados do documento gravados junto com cada chunk.
/// </summary>
public record MetadadosDocumento(
    string IdAgente,
    string CodigoDocumento,
    string NomeDocumento,
    DateOnly DataValidade,
    string Usuario);

public interface IRepositorioChunks
{
    /// <summary>
    /// Substitui todos os chunks do documento pelos novos, numa única transação.
    /// Retorna a quantidade de chunks antigos removidos.
    /// </summary>
    Task<int> SubstituirDocumentoAsync(
        MetadadosDocumento documento,
        IReadOnlyList<ChunkIndexado> chunks,
        CancellationToken cancellationToken);

    /// <summary>
    /// Busca os chunks do agente mais semelhantes ao embedding da consulta (similaridade do cosseno),
    /// ignorando documentos com validade vencida em <paramref name="dataReferencia"/>.
    /// </summary>
    Task<IReadOnlyList<TrechoEncontrado>> BuscarMaisSimilaresAsync(
        string idAgente,
        float[] consulta,
        int quantidade,
        DateOnly dataReferencia,
        CancellationToken cancellationToken);
}

/// <summary>
/// Grava os chunks na tabela <c>chunks_documento</c> do PostgreSQL com pgvector.
/// </summary>
public sealed class RepositorioChunksPostgres(NpgsqlDataSource dataSource) : IRepositorioChunks
{
    private const string SqlExcluir = "DELETE FROM chunks_documento WHERE codigo_documento = $1";

    private const string SqlInserir = """
        INSERT INTO chunks_documento
            (id_agente, codigo_documento, nome_documento, indice_chunk, data_validade, usuario, texto, embedding)
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
        """;

    // "<=>" é a distância do cosseno do pgvector; similaridade = 1 - distância.
    // Ordenar pela distância permite usar o índice HNSW.
    private const string SqlBuscar = """
        SELECT nome_documento, indice_chunk, texto, 1 - (embedding <=> $1) AS similaridade
        FROM chunks_documento
        WHERE id_agente = $2
          AND (data_validade IS NULL OR data_validade >= $3)
        ORDER BY embedding <=> $1
        LIMIT $4
        """;

    public async Task<int> SubstituirDocumentoAsync(
        MetadadosDocumento documento,
        IReadOnlyList<ChunkIndexado> chunks,
        CancellationToken cancellationToken)
    {
        await using var conexao = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transacao = await conexao.BeginTransactionAsync(cancellationToken);

        int removidos;
        await using (var excluir = new NpgsqlCommand(SqlExcluir, conexao, transacao))
        {
            excluir.Parameters.Add(new NpgsqlParameter { Value = documento.CodigoDocumento });
            removidos = await excluir.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var lote = new NpgsqlBatch(conexao, transacao))
        {
            foreach (var item in chunks)
            {
                lote.BatchCommands.Add(new NpgsqlBatchCommand(SqlInserir)
                {
                    Parameters =
                    {
                        new NpgsqlParameter { Value = documento.IdAgente },
                        new NpgsqlParameter { Value = documento.CodigoDocumento },
                        new NpgsqlParameter { Value = documento.NomeDocumento },
                        new NpgsqlParameter { Value = item.Chunk.Indice },
                        new NpgsqlParameter { Value = documento.DataValidade },
                        new NpgsqlParameter { Value = documento.Usuario },
                        new NpgsqlParameter { Value = item.Chunk.Texto },
                        new NpgsqlParameter { Value = new Vector(item.Embedding) },
                    }
                });
            }

            await lote.ExecuteNonQueryAsync(cancellationToken);
        }

        await transacao.CommitAsync(cancellationToken);

        return removidos;
    }

    public async Task<IReadOnlyList<TrechoEncontrado>> BuscarMaisSimilaresAsync(
        string idAgente,
        float[] consulta,
        int quantidade,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
    {
        await using var comando = dataSource.CreateCommand(SqlBuscar);
        comando.Parameters.Add(new NpgsqlParameter { Value = new Vector(consulta) });
        comando.Parameters.Add(new NpgsqlParameter { Value = idAgente });
        comando.Parameters.Add(new NpgsqlParameter { Value = dataReferencia });
        comando.Parameters.Add(new NpgsqlParameter { Value = quantidade });

        var trechos = new List<TrechoEncontrado>(quantidade);

        await using var leitor = await comando.ExecuteReaderAsync(cancellationToken);
        while (await leitor.ReadAsync(cancellationToken))
        {
            trechos.Add(new TrechoEncontrado(
                new Chunk(leitor.GetString(0), leitor.GetInt32(1), leitor.GetString(2)),
                leitor.GetDouble(3)));
        }

        return trechos;
    }
}
