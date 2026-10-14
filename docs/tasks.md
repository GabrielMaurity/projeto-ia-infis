# Tasks

## Etapa 1

- criar solution
- criar Api.Atendimento
- criar Api.Conhecimento
- configurar Swagger

## Etapa 2

- transformar Api.Conhecimento em MCP Server
- criar tool BuscarConhecimento

## Etapa 3

- criar MCP Client na Api.Atendimento
- testar descoberta das tools
- testar chamada MCP

## Etapa 4

- implementar serviço de embeddings
- implementar chunking
- implementar armazenamento vetorial
- implementar busca por similaridade

## Etapa 5

- integrar OpenAI
- implementar Structured Output

## Etapa 6

- registrar tokens
- calcular custo

## Etapa 7

- implementar Gemini
- criar fallback

## Etapa 8

- Criar uma flag na saída estruturada que indique se o atendimento deve ser encaminhado para um suporte humano
- Modificar o prompt para identificar a necessidade anterior sob 2 condições: 
  ou o usuário pede explicitamente para ser atendimento por um humano ou o agente identifica 
  que o usuário está insatisfeito com o atendimento.
- Crie um função usando Kernel Semantico para que o agente possa executar o encaminhamento do atendimento
  Essa função deverá apenas gravar um registro em uma OutboxMessage no SqlServer contendo:
	Id sequencial
	Data e hora de gravação
	Transmitido (default false)
	Data e hora da transmissão (default null)
	Nome da fila (default null)
	Usuário
	Descrição (conteudo gerado pela IA com o resumo do atendimento)

## Etapa 9

- Instale um container do RabbitMQ no Docker
- Crie um serviço (BackgroundService) para, a cada 1 minuto, fazer as tarefas:
	- Leia os 5 primeiros registros da tabela OutboxMessage que não foram transmitidos
	- Enviar para a fila 'atendimentos' um JSON estruturado com:
		- Id
		- Nome do usuário
		- Descrição
	- Atualiza a tabela OutboxMessage com o status da transmissão
- Instale um container do Mailhog no Docker
	- Crie um projeto Worker Service na solução que leia os registros da fila do RabbitMQ
	- Para cada registro lido faça:
		- Grave o historico da leitura em um tabela InboxMessage com os campos:
			Id sequencial
			Id da mensagem da fila
	        Data e hora
	        Nome da fila
	        Usuário
	        Descrição
			Status do envio do email
	- Faça com que cada registro lido da fila seja enviado para o mailhog usando:
		- Remetente: worker-service@infis.com.br
		- Destinatário: atendimento-suporte@infis.com.br
		- Assunto: Novo atendimento solicitado por: {usuario}
		- Mensagem:
			- Escreva uma mensagem contendo a descrição com um layout HTML padrão