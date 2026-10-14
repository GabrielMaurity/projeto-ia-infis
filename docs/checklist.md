# Checklist do projeto

Situação das tarefas da solução **CorporativoAI** (Api.Atendimento + Api.Conhecimento).
As etapas seguem `docs/tasks.md`; os itens extras vieram das solicitações feitas durante o desenvolvimento.

> **Manutenção**: este arquivo deve ser atualizado a cada interação que implemente, altere ou
> identifique uma tarefa (instrução registrada no `CLAUDE.md` da raiz).
> Legenda: `[x]` implementado · `[ ]` pendente · `[~]` parcial.

**Última atualização:** 2026-10-14 (README do projeto)

## Etapa 1 — Estrutura

- [x] Solution `CorporativoAI.sln`
- [x] Projeto Api.Atendimento
- [x] Projeto Api.Conhecimento
- [x] Swagger nas duas APIs (com comentários XML)

## Etapa 2 — MCP Server

- [x] Api.Conhecimento como MCP Server (`ModelContextProtocol.AspNetCore`, transporte HTTP, rota `/mcp`)
- [x] Tool `BuscarConhecimento`

## Etapa 3 — MCP Client

- [x] MCP Client na Api.Atendimento (`ConhecimentoMcpClient`, conexão reaproveitada)
- [x] Descoberta das tools (`GET /api/conhecimento/ferramentas`)
- [x] Chamada da tool via MCP (`POST /api/conhecimento/buscar`)

## Etapa 4 — RAG

- [x] Geração de embeddings (OpenAI `text-embedding-3-small`)
- [x] Chunking por parágrafos/títulos, sem partir palavras
- [x] Armazenamento vetorial — inicialmente em memória, hoje no **PostgreSQL + pgvector**
- [x] Busca por similaridade do cosseno (operador `<=>` + índice HNSW), filtrando agente e validade

## Etapa 5 — OpenAI

- [x] Integração com o SDK oficial da OpenAI (`gpt-4.1-mini`)
- [x] Structured Output (JSON Schema estrito, categoria em `enum`)

## Etapa 6 — Tokens e custos

- [x] Tokens de entrada e saída na resposta
- [x] Custo estimado (entrada + saída, preços configuráveis)
- [x] Registro do consumo por requisição no SQL Server (`bd-custos-ia.dbo.ConsumoTokens`)

## Etapa 7 — Fallback

- [x] Integrar o Gemini (`gemini-3.5-flash-lite`, SDK oficial `Google.GenAI`) como modelo de contingência
- [x] Chamar o Gemini somente quando a OpenAI falhar (erro, timeout de 30 s, chave ausente) e preencher `fallback: true`
- [x] Mesmo prompt e mesmo JSON Schema de saída nos dois provedores
- [x] Preços do Gemini no cálculo de custo (US$ 0,30/M entrada, US$ 2,50/M saída, incluindo tokens de raciocínio)
- [x] HTTP 503 quando OpenAI e Gemini falham
- [x] Chave da Gemini API (`Gemini:ApiKey`) configurada nos user-secrets da Api.Atendimento

## Etapa 8 — Encaminhamento para suporte humano

- [x] Flag `encaminharSuporteHumano` na saída estruturada (JSON Schema da OpenAI e do Gemini), com `resumoEncaminhamento` para o atendente
- [x] Prompt com as 2 condições: pedido explícito de atendimento humano ou insatisfação do usuário (pergunta atual ou histórico)
- [x] Função do Semantic Kernel `Atendimento.EncaminharParaSuporteHumano` (`EncaminhamentoPlugin`), executada pelo agente quando o flag vem `true`
- [x] Tabela `dbo.OutboxMessage` no SQL Server (Id, data/hora de gravação, Transmitido = 0, data/hora da transmissão e nome da fila nulos, usuário, descrição)
- [x] Flag devolvido na resposta da API e gravado no histórico do MongoDB
- [x] Falha ao gravar na outbox não impede a resposta (resumo vai para o log)
- [x] Tabela `OutboxMessage` criada no ambiente existente (container `sqlserver` reiniciado)
- [x] Processo de envio da outbox (Etapa 9)

## Etapa 9 — Fila de encaminhamentos (RabbitMQ, Worker e e-mail)

- [x] Container do RabbitMQ (`rabbitmq-atendimentos`, AMQP `5672`, painel `15672`, `infis`/`infis`)
- [x] `BackgroundService` na Api.Atendimento (`PublicacaoOutboxHostedService`) executado a cada 1 minuto
- [x] Lê as 5 primeiras `OutboxMessage` não transmitidas e publica na fila `atendimentos` o JSON `{ id, nomeUsuario, descricao }`
- [x] Atualiza a `OutboxMessage` com `Transmitido = 1`, `DataHoraTransmissao` e `NomeFila` após a confirmação do broker (publisher confirms)
- [x] Container do Mailhog (`mailhog-atendimentos`, SMTP `1025`, web `8025`)
- [x] Projeto Worker Service `Worker.Suporte` na solução, consumindo a fila `atendimentos` (ack manual, prefetch 1)
- [x] Tabela `dbo.InboxMessage` (Id, Id da mensagem da fila, data/hora, nome da fila, usuário, descrição, status do envio do e-mail)
- [x] E-mail para o Mailhog: de `worker-service@infis.com.br` para `atendimento-suporte@infis.com.br`, assunto `Novo atendimento solicitado por: {usuario}`, corpo em layout HTML padrão
- [x] Reentregas tratadas pela inbox (status `Duplicada`, sem reenviar o e-mail); falha no SMTP devolve a mensagem à fila
- [x] `docs/worker-suporte.md`
- [ ] Criar a tabela `InboxMessage` no ambiente existente: `docker compose restart sqlserver`
- [ ] Fila de mensagens inválidas (*dead-letter queue*): hoje o JSON inválido é descartado e fica só no log

## Testes automatizados

- [x] `tests/Api.Conhecimento.Tests` — validação de PDF, extração, chunking, ingestão, similaridade
- [x] `tests/Api.Atendimento.Tests` — prompt, histórico, custo, saída estruturada, fallback, encaminhamento (plugin do Semantic Kernel), publicação da outbox, orquestração e cenários de falha
- [x] `tests/Worker.Suporte.Tests` — leitura da mensagem, modelo do e-mail e processamento (nova, duplicada, falha no SMTP, JSON inválido)
- [ ] Testes de integração dos repositórios (PostgreSQL, MongoDB, SQL Server) e do RabbitMQ
- [ ] Testes de API com `WebApplicationFactory` (endpoints e respostas de erro)

## Documentação

- [x] `docs/atendimento.md`
- [x] `docs/ingest.md`
- [x] `docs/worker-suporte.md`
- [x] `docs/checklist.md`
- [ ] Atualizar `docs/architecture.md` e `docs/specifications.md` (ainda dizem "embeddings em memória"; faltam MongoDB, SQL Server, ingestão de PDFs, RabbitMQ e Worker.Suporte)
- [x] `README.md` com visão geral, arquitetura, como executar, testes e links para a documentação

## Etapa 10 — Revisão

- [ ] Revisão geral de código

## Infraestrutura (Docker)

- [x] `docker-compose.yml` com rede `rede-rag-infis`, volumes e health checks
- [x] PostgreSQL + pgvector (`5433`) e pgAdmin (`5051`, servidor pré-cadastrado)
- [x] MongoDB (`27018`) e Mongo Express (`5052`)
- [x] SQL Server (`1434`) e Adminer (`5053`, tema `pepa-linha`)
- [x] RabbitMQ (`5672`, painel `15672`) e Mailhog (`1025`, web `8025`)
- [x] Scripts de inicialização: tabela `chunks_documento`, collection `atendimentos` + índice, banco `bd-custos-ia` + tabelas `ConsumoTokens`, `OutboxMessage` e `InboxMessage`
- [x] Variáveis em `.env` (modelo em `.env.example`)
- [~] Arquivos do Docker na pasta "docker" da solution (faltam os do SQL Server e o `.gitattributes`)

## Ingestão de PDFs (Api.Conhecimento)

- [x] `POST /api/v1/ingest` (usuário, código, validade, PDF)
- [x] Validação: PDF real (assinatura `%PDF-`) e até 2 MB
- [x] Extração de texto (PdfPig), chunks, embeddings e gravação em transação
- [x] Reenvio do mesmo código substitui o documento
- [x] Respostas `{ status, mensagem }` (201, 400, 422, 502, 503)
- [x] Logs de cada passo
- [ ] Endpoints para listar e excluir documentos ingeridos

## Atendimento (Api.Atendimento)

- [x] `POST /api/v1/atendimentos` (nome do usuário + pergunta)
- [x] Assistente restrito a dúvidas tributárias e contábeis (prompt com regras e proteção contra *prompt injection*)
- [x] Contexto obtido da Api.Conhecimento exclusivamente via MCP
- [x] Resposta estruturada (resposta, categoria, fontes, modelo, fallback, tokens, custo)
- [x] Fallback para o Gemini quando a OpenAI falha (Etapa 7)
- [x] Encaminhamento para suporte humano via Semantic Kernel + outbox (Etapa 8)
- [x] Memória conversacional: gravação de cada interação no MongoDB
- [x] Memória conversacional: últimas 3 conversas do usuário enviadas no prompt
- [x] Controle de consumo de tokens com referência ao Id da conversa no MongoDB
- [x] Falhas no MongoDB/SQL Server não impedem a entrega da resposta
- [x] Falha da OpenAI tratada: fallback para o Gemini; se os dois falharem, HTTP 503 com mensagem clara
- [ ] Usar o histórico também na busca do RAG (perguntas de continuação como "e para o Simples?")
- [ ] Identificar o usuário por um identificador estável (login/e-mail) em vez do nome exato
- [ ] Endpoints de consulta do histórico e do consumo/custos

## Segurança e configuração

- [x] Chaves da OpenAI e do Gemini fora do código: user-secrets das duas APIs, `"ApiKey": ""` nos `appsettings.json`
      (em outros ambientes, usar as variáveis `OpenAI__ApiKey` e `Gemini__ApiKey`)
- [ ] (Opcional) Gerar novas chaves na OpenAI e no Google AI Studio: as atuais nunca foram para o Git, mas
      apareceram em saídas de ferramentas durante o desenvolvimento
- [ ] Corrigir a vulnerabilidade alta (GHSA-846f-f764-76mx) do pacote transitivo `System.Security.Cryptography.Pkcs`,
      trazido pelo `Microsoft.Data.SqlClient` 7.1.0 (Api.Atendimento e Worker.Suporte) e pelo `MimeKit` 4.18.1 (Worker.Suporte):
      referenciar diretamente uma versão corrigida do pacote
- [ ] Usuários dedicados no SQL Server (Api.Atendimento e Worker.Suporte só com as permissões necessárias) e no RabbitMQ, em vez de `sa` e do usuário administrador
- [ ] Conflito da porta `1434` com o SQL Server local (DAC do `MSSQLSERVER`) — parar o serviço local ou trocar a porta

## Limpeza

- [ ] Remover o RAG em memória que não é mais usado: `ArmazenamentoVetorialEmMemoria`,
      `IndexadorDocumentos`, `LeitorDocumentos`, `Documentos/coti-info.md` e os testes correspondentes
- [x] Código versionado em commits por responsabilidade na `main` local
- [ ] Publicar no GitHub: `git push origin main`
