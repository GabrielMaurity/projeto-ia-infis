# Arquitetura

## Solução

CorporativoAI.sln

Projetos:

- Api.Atendimento
- Api.Conhecimento

## Api.Atendimento

Responsabilidades:

REST API
Orquestração
MCP Client
OpenAI
Gemini fallback
Structured Outputs
Controle de tokens
Controle de custos

## Api.Conhecimento

Responsabilidades:

MCP Server
Ingestão de documentos
Chunking
Embeddings
Busca vetorial
RAG

## Fluxo

Cliente
→ Api.Atendimento
→ MCP Client
→ Api.Conhecimento
→ Embeddings
→ Busca vetorial
→ MCP
→ Api.Atendimento
→ OpenAI
→ Resposta
