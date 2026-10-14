-- Chunks dos documentos indexados para a busca vetorial (RAG).
CREATE TABLE IF NOT EXISTS chunks_documento (
    id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_agente        VARCHAR(100)  NOT NULL,
    codigo_documento VARCHAR(100)  NOT NULL,
    nome_documento   VARCHAR(255)  NOT NULL,
    indice_chunk     INT           NOT NULL,
    data_inclusao    TIMESTAMPTZ   NOT NULL DEFAULT now(),
    data_validade    DATE          NULL,
    usuario          VARCHAR(100)  NOT NULL,
    texto            TEXT          NOT NULL,
    -- 1536 dimensões = text-embedding-3-small (OpenAI:ModeloEmbedding).
    embedding        VECTOR(1536)  NOT NULL,

    CONSTRAINT uq_chunks_documento_codigo_indice UNIQUE (codigo_documento, indice_chunk)
);

COMMENT ON TABLE  chunks_documento IS 'Chunks dos documentos indexados para busca vetorial.';
COMMENT ON COLUMN chunks_documento.id_agente        IS 'Agente dono do conhecimento.';
COMMENT ON COLUMN chunks_documento.codigo_documento IS 'Código único do documento de origem.';
COMMENT ON COLUMN chunks_documento.indice_chunk     IS 'Posição do chunk dentro do documento (começa em 0).';
COMMENT ON COLUMN chunks_documento.data_validade    IS 'Nulo = não expira.';
COMMENT ON COLUMN chunks_documento.usuario          IS 'Usuário que incluiu o documento.';

-- Busca por similaridade de cosseno.
CREATE INDEX IF NOT EXISTS ix_chunks_documento_embedding
    ON chunks_documento USING hnsw (embedding vector_cosine_ops);

CREATE INDEX IF NOT EXISTS ix_chunks_documento_id_agente
    ON chunks_documento (id_agente);

CREATE INDEX IF NOT EXISTS ix_chunks_documento_data_validade
    ON chunks_documento (data_validade);
