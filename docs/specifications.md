	# Specifications

## .NET

Utilizar ASP.NET Core.

## MCP

A API Conhecimento deverá utilizar:

ModelContextProtocol.AspNetCore

O transporte deverá ser HTTP.

Rota:

/mcp

A API Atendimento será MCP Client.

## OpenAI

Utilizar o SDK oficial OpenAI para .NET.

A chave deverá vir de configuração segura.

## Gemini

Utilizar Gemini somente como modelo de contingência.

OpenAI será sempre chamada primeiro.

Gemini somente será chamada quando OpenAI falhar.

## RAG

Os documentos estarão inicialmente na pasta:

/Documentos

Cada documento será dividido em pequenos trechos.

Os embeddings ficarão inicialmente armazenados em memória.

A similaridade será calculada através de similaridade do cosseno.

## Resposta

A resposta deverá conter:

resposta
categoria
fontes
modelo
fallback
tokensEntrada
tokensSaida
custoEstimado
