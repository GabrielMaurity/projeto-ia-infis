#!/bin/bash
# Sobe o SQL Server e, quando ele aceitar conexões, executa os scripts de init.
# A imagem do SQL Server não tem pasta docker-entrypoint-initdb.d como Postgres e MongoDB;
# os scripts são idempotentes (IF NOT EXISTS) e rodam a cada inicialização.

/opt/mssql/bin/sqlservr &
PID_SQLSERVR=$!

SQLCMD=/opt/mssql-tools18/bin/sqlcmd

executar_scripts() {
    for script in /docker-init/*.sql; do
        echo "Executando $script"
        "$SQLCMD" -C -I -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -b -i "$script" || return 1
    done
}

# Logo após aceitar conexões o SQL Server ainda atualiza os bancos de sistema e recusa
# CREATE DATABASE ("cannot be autostarted during server startup"): tenta novamente.
for tentativa in $(seq 1 60); do
    if executar_scripts; then
        echo "Inicialização do banco concluída."
        break
    fi
    echo "SQL Server ainda não está pronto para os scripts de init ($tentativa/60)..."
    sleep 3
done

wait $PID_SQLSERVR
