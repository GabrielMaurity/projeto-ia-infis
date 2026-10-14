# CorporativoAI

Solução .NET 10 com duas APIs que se comunicam por MCP: **Api.Atendimento** (MCP Client, assistente
tributário e contábil) e **Api.Conhecimento** (MCP Server, ingestão de PDFs e RAG com pgvector), mais o
**Worker.Suporte** (consome a fila de encaminhamentos do RabbitMQ e envia e-mails ao suporte).
Documentação em `docs/` (`atendimento.md`, `ingest.md`, `worker-suporte.md`, `requirementes.md`,
`specifications.md`, `tasks.md`).

## Checklist

- `docs/checklist.md` registra o que está implementado e o que está pendente.
- Ao final de toda interação que implemente, altere, remova ou identifique uma tarefa, atualize o
  `docs/checklist.md`: marque `[x]`/`[~]`/`[ ]`, inclua itens novos na seção adequada e ajuste a data
  de "Última atualização".
- Se a mudança afetar o que está descrito em `docs/atendimento.md`, `docs/ingest.md` ou
  `docs/worker-suporte.md`, atualize esses arquivos também.
