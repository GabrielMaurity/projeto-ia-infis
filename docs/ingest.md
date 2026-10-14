# Ingestão de PDFs

A **Api.Conhecimento** recebe documentos PDF, divide o texto em chunks, gera os embeddings e grava tudo
no banco RAG (PostgreSQL + pgvector). Esses chunks são a base usada pela tool MCP `BuscarConhecimento`
para responder às perguntas da Api.Atendimento.

- Endpoint: `POST /api/v1/ingest` (`multipart/form-data`)
- Porta local: `http://localhost:5026` (Swagger em `/swagger`)
- Código principal: `src/Api.Conhecimento/Ingestao/IngestaoDocumentos.cs`

## Processamento

Requisição:

| Campo | Tipo | Regra |
|---|---|---|
| `NomeUsuario` | texto | Obrigatório, até 100 caracteres |
| `CodigoDocumento` | texto | Obrigatório, até 100 caracteres; identifica o documento |
| `Validade` | data `yyyy-MM-dd` | Obrigatória, hoje ou futura |
| `Arquivo` | arquivo | PDF com no máximo 2 MB |

Cada etapa é registrada no log com o código do documento:

```
[DOC-RH-001] Passo 1/5: validando o arquivo politicas-rh.pdf.
[DOC-RH-001] Passo 2/5: extraindo o texto do PDF (29613 bytes).
[DOC-RH-001] Passo 3/5: dividindo 3114 caractere(s) em chunks.
[DOC-RH-001] Passo 4/5: gerando embeddings de 5 chunk(s).
[DOC-RH-001] Passo 5/5: gravando 5 chunk(s) no banco RAG.
[DOC-RH-001] Ingestão concluída: 5 chunk(s) gravado(s).
```

1. **Validação** (`Ingestao/ValidadorArquivoPdf.cs`): arquivo não vazio, até
   `Ingestao:TamanhoMaximoArquivoBytes` (2 MB), extensão `.pdf` **e** conteúdo começando com a assinatura
   `%PDF-` (um arquivo renomeado para `.pdf` é recusado). A validade não pode estar no passado.
2. **Extração do texto** (`Ingestao/ExtratorTextoPdf.cs`): biblioteca **PdfPig**, na ordem de leitura de
   cada página; páginas separadas por linha em branco. PDFs escaneados (só imagem) não têm texto extraível.
3. **Chunking** (`Rag/DivisorChunks.cs`): agrupa parágrafos até `Rag:TamanhoMaximoChunk` (800 caracteres),
   repetindo o título da seção quando houver; trechos maiores são cortados no último espaço antes do
   limite, sem partir palavras.
4. **Embeddings** (`Rag/GeradorEmbeddings.cs`): OpenAI `text-embedding-3-small` (1536 dimensões), em lotes de 100.
5. **Gravação** (`Ingestao/RepositorioChunks.cs`): em **uma transação**, apaga os chunks existentes do
   mesmo `CodigoDocumento` e insere os novos. Reenviar um código já cadastrado **substitui** o documento.

**Tabela `chunks_documento`** (banco `bd-rag-infis`, criada por `docker/postgres/init/02-tabela-chunks.sql`):

| Coluna | Tipo | Origem |
|---|---|---|
| `id` | `BIGINT IDENTITY` (PK) | Sequencial |
| `id_agente` | `VARCHAR(100)` | `Ingestao:IdAgente` |
| `codigo_documento` | `VARCHAR(100)` | `CodigoDocumento` |
| `nome_documento` | `VARCHAR(255)` | Nome do arquivo enviado |
| `indice_chunk` | `INT` | Posição do chunk no documento (único junto com `codigo_documento`) |
| `data_inclusao` | `TIMESTAMPTZ` | `now()` |
| `data_validade` | `DATE` | `Validade` |
| `usuario` | `VARCHAR(100)` | `NomeUsuario` |
| `texto` | `TEXT` | Texto do chunk |
| `embedding` | `VECTOR(1536)` | Embedding do chunk (índice HNSW, distância do cosseno) |

**Respostas** — sempre no formato `{ "status": ..., "mensagem": "..." }`:

| Situação | Status | Mensagem |
|---|---|---|
| Sucesso | **201** | Conhecimento cadastrado com sucesso |
| Campo ausente/inválido | 400 | Lista dos campos (ex.: "Envie o arquivo PDF. Informe a validade do documento.") |
| Arquivo maior que 2 MB | 400 | O arquivo excede o tamanho máximo de 2 MB. |
| Não é PDF | 400 | O arquivo enviado não é um PDF. |
| Validade vencida | 400 | A validade deve ser hoje ou uma data futura. |
| PDF corrompido ou protegido por senha | 422 | Não foi possível ler o PDF... |
| PDF sem texto (escaneado) | 422 | O PDF não possui texto extraível... |
| Falha na OpenAI | 502 | Não foi possível gerar os embeddings do documento... |
| Falha no PostgreSQL | 503 | Não foi possível gravar o documento na base de conhecimento... |

A requisição inteira é limitada a 10 MB (acima disso o Kestrel devolve 413 sem corpo); o limite de 2 MB
do PDF é validado pela API para devolver uma mensagem clara.

**Uso na busca**: a tool MCP `BuscarConhecimento` (`Tools/ConhecimentoTools.cs` → `Rag/BuscaConhecimento.cs`)
gera o embedding da pergunta e retorna os `Rag:QuantidadeResultados` chunks mais semelhantes do mesmo
`id_agente`, ignorando documentos com `data_validade` anterior a hoje.

## Configuração

`src/Api.Conhecimento/appsettings.json` e `appsettings.Development.json`:

| Chave | Padrão | Descrição |
|---|---|---|
| `ConnectionStrings:RagInfis` | `Host=localhost;Port=5433;Database=bd-rag-infis;Username=infis;Password=infis` | Banco RAG (em `appsettings.Development.json`); obrigatória |
| `OpenAI:ApiKey` | — | Chave da OpenAI (prefira user-secrets ou a variável `OpenAI__ApiKey`) |
| `OpenAI:ModeloEmbedding` | `text-embedding-3-small` | Modelo de embeddings; mudar a dimensão exige alterar `VECTOR(1536)` |
| `Rag:TamanhoMaximoChunk` | `800` | Tamanho máximo do chunk, em caracteres (100 a 8000) |
| `Rag:QuantidadeResultados` | `3` | Chunks devolvidos pela busca (1 a 20) |
| `Ingestao:IdAgente` | `agente-corporativo` | Agente dono dos documentos ingeridos e filtrado na busca |
| `Ingestao:TamanhoMaximoArquivoBytes` | `2097152` (2 MB) | Tamanho máximo do PDF |

A seção `Ingestao` é opcional: sem ela valem os padrões acima.

Pacotes: `PdfPig` 0.1.16, `Pgvector` 0.3.2 e `Npgsql.DependencyInjection` 9.0.5 — o Npgsql fica na
linha 9.x porque o Pgvector 0.3.2 não é compatível com o Npgsql 10.

Infraestrutura (`docker-compose.yml`): container `postgres-rag-infis` (imagem `pgvector/pgvector:pg17`,
porta `5433`) e `pgadmin-rag-infis` (porta `5051`, servidor já cadastrado). Os scripts de
`docker/postgres/init` só rodam na primeira criação do volume; para recriar do zero:
`docker compose down -v` (**apaga os dados**).

## Execução e testes

1. Subir o banco e configurar a chave:

   ```bash
   docker compose up -d postgres pgadmin
   ```

   ```bash
   dotnet user-secrets set "OpenAI:ApiKey" "<chave>" --project src/Api.Conhecimento
   ```

2. Subir a API:

   ```bash
   dotnet run --project src/Api.Conhecimento --launch-profile http
   ```

3. Enviar um PDF pelo Swagger (`http://localhost:5026/swagger`), pelo arquivo
   `src/Api.Conhecimento/Api.Conhecimento.http` ou via curl:

   ```bash
   curl -X POST http://localhost:5026/api/v1/ingest -F NomeUsuario=gabriel -F CodigoDocumento=DOC-RH-001 -F Validade=2027-12-31 -F "Arquivo=@documento.pdf;type=application/pdf"
   ```

**Validação manual**:

| Cenário | Esperado |
|---|---|
| PDF com texto, até 2 MB | 201 — Conhecimento cadastrado com sucesso |
| Mesmo `CodigoDocumento` enviado de novo | 201 — chunks substituídos (log "chunk(s) da versão anterior... substituídos") |
| PDF acima de 2 MB | 400 |
| Arquivo `.txt` renomeado para `.pdf` | 400 |
| PDF escaneado / sem texto | 422 |
| Validade no passado | 400 |
| Campos vazios | 400 com a lista dos campos |

Conferência no pgAdmin (`http://localhost:5051`) ou via `psql`:

```sql
SELECT codigo_documento, nome_documento, data_validade, usuario, count(*) AS chunks
FROM chunks_documento
GROUP BY 1, 2, 3, 4;
```

```bash
docker exec -it postgres-rag-infis psql -U infis -d bd-rag-infis
```

**Testes automatizados**:

```bash
dotnet test tests/Api.Conhecimento.Tests
```

Cobrem o validador de PDF (tamanho, extensão, assinatura), a extração de texto (PDFs gerados em
memória com o PdfPig), o divisor de chunks (sem partir palavras) e o fluxo de ingestão (sucesso,
validade vencida, arquivo inválido, PDF sem texto ou ilegível, falha nos embeddings e no banco).
