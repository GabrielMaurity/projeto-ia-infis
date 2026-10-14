-- Executado a cada inicialização do container: todas as instruções são idempotentes.
-- Coluna calculada PERSISTED e índice filtrado exigem estas opções ligadas.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF DB_ID(N'bd-custos-ia') IS NULL
    CREATE DATABASE [bd-custos-ia];
GO

USE [bd-custos-ia];
GO

-- Consumo de tokens de cada requisição da Api.Atendimento. Custos em dólares (USD).
IF OBJECT_ID(N'dbo.ConsumoTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsumoTokens
    (
        Id            BIGINT IDENTITY(1, 1) NOT NULL,
        Agente        NVARCHAR(100)  NOT NULL,
        Modelo        NVARCHAR(100)  NOT NULL,
        Usuario       NVARCHAR(100)  NOT NULL,
        DataHora      DATETIME2(3)   NOT NULL CONSTRAINT DF_ConsumoTokens_DataHora DEFAULT SYSUTCDATETIME(),
        TokensEntrada INT            NOT NULL,
        TokensSaida   INT            NOT NULL,
        CustoEntrada  DECIMAL(18, 8) NOT NULL,
        CustoSaida    DECIMAL(18, 8) NOT NULL,
        -- Calculado pelo banco, sempre igual à soma das parcelas.
        CustoTotal    AS (CustoEntrada + CustoSaida) PERSISTED,
        -- _id (ObjectId) da conversa na collection atendimentos do MongoDB;
        -- nulo quando a conversa não pôde ser gravada.
        IdConversa    CHAR(24)       NULL,

        CONSTRAINT PK_ConsumoTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ConsumoTokens_Tokens CHECK (TokensEntrada >= 0 AND TokensSaida >= 0),
        CONSTRAINT CK_ConsumoTokens_Custos CHECK (CustoEntrada >= 0 AND CustoSaida >= 0)
    );

    CREATE INDEX IX_ConsumoTokens_DataHora ON dbo.ConsumoTokens (DataHora) INCLUDE (CustoTotal);
    CREATE INDEX IX_ConsumoTokens_Usuario_DataHora ON dbo.ConsumoTokens (Usuario, DataHora);
    CREATE INDEX IX_ConsumoTokens_IdConversa ON dbo.ConsumoTokens (IdConversa) WHERE IdConversa IS NOT NULL;
END
GO

-- Outbox de encaminhamentos para o suporte humano. Cada linha é gravada pela função do
-- Semantic Kernel Atendimento.EncaminharParaSuporteHumano; um processo de envio (ainda não
-- implementado) publica as mensagens pendentes na fila e marca Transmitido = 1.
IF OBJECT_ID(N'dbo.OutboxMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxMessage
    (
        Id                  BIGINT IDENTITY(1, 1) NOT NULL,
        DataHoraGravacao    DATETIME2(3)   NOT NULL CONSTRAINT DF_OutboxMessage_DataHoraGravacao DEFAULT SYSUTCDATETIME(),
        Transmitido         BIT            NOT NULL CONSTRAINT DF_OutboxMessage_Transmitido DEFAULT 0,
        DataHoraTransmissao DATETIME2(3)   NULL     CONSTRAINT DF_OutboxMessage_DataHoraTransmissao DEFAULT NULL,
        NomeFila            NVARCHAR(200)  NULL     CONSTRAINT DF_OutboxMessage_NomeFila DEFAULT NULL,
        Usuario             NVARCHAR(100)  NOT NULL,
        -- Resumo do atendimento gerado pela IA para o atendente humano.
        Descricao           NVARCHAR(MAX)  NOT NULL,

        CONSTRAINT PK_OutboxMessage PRIMARY KEY CLUSTERED (Id)
    );

    -- Mensagens ainda não transmitidas, na ordem de gravação.
    CREATE INDEX IX_OutboxMessage_Pendentes ON dbo.OutboxMessage (Id) WHERE Transmitido = 0;
END
GO

-- Histórico das mensagens lidas da fila pelo Worker.Suporte (padrão inbox). Uma linha por leitura:
-- reentregas da mesma mensagem geram novas linhas, e a que já tinha e-mail enviado fica como 'Duplicada'.
IF OBJECT_ID(N'dbo.InboxMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InboxMessage
    (
        Id               BIGINT IDENTITY(1, 1) NOT NULL,
        -- MessageId da mensagem no RabbitMQ (o Id da OutboxMessage de origem).
        IdMensagemFila   NVARCHAR(100)  NOT NULL,
        DataHora         DATETIME2(3)   NOT NULL CONSTRAINT DF_InboxMessage_DataHora DEFAULT SYSUTCDATETIME(),
        NomeFila         NVARCHAR(200)  NOT NULL,
        Usuario          NVARCHAR(100)  NOT NULL,
        Descricao        NVARCHAR(MAX)  NOT NULL,
        StatusEnvioEmail NVARCHAR(20)   NOT NULL CONSTRAINT DF_InboxMessage_StatusEnvioEmail DEFAULT N'Pendente',

        CONSTRAINT PK_InboxMessage PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_InboxMessage_StatusEnvioEmail
            CHECK (StatusEnvioEmail IN (N'Pendente', N'Enviado', N'Falha', N'Duplicada'))
    );

    CREATE INDEX IX_InboxMessage_IdMensagemFila ON dbo.InboxMessage (IdMensagemFila) INCLUDE (StatusEnvioEmail);
END
GO
