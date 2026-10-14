# Requirements

## Objetivo

Desenvolver uma solução corporativa utilizando .NET
composta por duas APIs que se comuniquem através
do Model Context Protocol.

## API Atendimento

A API Atendimento deverá:

- receber perguntas através de REST;
- atuar como aplicação orquestradora;
- consultar a API Conhecimento através de MCP;
- utilizar OpenAI como LLM principal;
- utilizar Gemini como fallback;
- retornar dados estruturados;
- registrar tokens consumidos;
- calcular custo estimado da chamada.

## API Conhecimento

A API Conhecimento deverá:

- funcionar como MCP Server;
- disponibilizar ferramenta para busca de conhecimento;
- carregar documentos corporativos;
- dividir documentos em chunks;
- gerar embeddings;
- executar busca vetorial;
- devolver os trechos mais relevantes.

## Requisitos não funcionais

- utilizar ASP.NET Core;
- utilizar async/await;
- utilizar Dependency Injection;
- não armazenar API Keys no código;
- utilizar CancellationToken;
- possuir logging;
- possuir tratamento de erros;
- possuir testes automatizados.