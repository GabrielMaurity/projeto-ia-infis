using Api.Atendimento.Configuracoes;
using Api.Atendimento.Custos;
using Api.Atendimento.Encaminhamento;
using Api.Atendimento.Erros;
using Api.Atendimento.Historico;
using Api.Atendimento.Servicos;
using Api.Atendimento.Servicos.Llm;
using Microsoft.OpenApi;
using Microsoft.SemanticKernel;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Api.Atendimento",
        Version = "v1",
        Description = "API de atendimento: recebe perguntas via REST e orquestra as chamadas aos modelos de IA."
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConhecimentoIndisponivelExceptionHandler>();
builder.Services.AddExceptionHandler<LlmIndisponivelExceptionHandler>();

builder.Services
    .AddOptions<ConhecimentoMcpOptions>()
    .Bind(builder.Configuration.GetSection(ConhecimentoMcpOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient(ConhecimentoMcpClient.NomeHttpClient);
builder.Services.AddSingleton<IConhecimentoMcpClient, ConhecimentoMcpClient>();

builder.Services
    .AddOptions<OpenAIOptions>()
    .Bind(builder.Configuration.GetSection(OpenAIOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<GeminiOptions>()
    .Bind(builder.Configuration.GetSection(GeminiOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// OpenAI é sempre chamada primeiro; o Gemini só responde quando a OpenAI falhar.
builder.Services.AddSingleton<OpenAILlmClient>();
builder.Services.AddSingleton<GeminiLlmClient>();
builder.Services.AddSingleton<ILlmClient>(sp => new LlmComFallback(
    sp.GetRequiredService<OpenAILlmClient>(),
    sp.GetRequiredService<GeminiLlmClient>(),
    sp.GetRequiredService<ILogger<LlmComFallback>>()));

builder.Services
    .AddOptions<AtendimentoOptions>()
    .Bind(builder.Configuration.GetSection(AtendimentoOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<HistoricoMongoOptions>()
    .Bind(builder.Configuration.GetSection(HistoricoMongoOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var conexaoHistorico = builder.Configuration.GetConnectionString("HistoricoAtendimentos");
if (string.IsNullOrWhiteSpace(conexaoHistorico))
{
    throw new InvalidOperationException("ConnectionStrings:HistoricoAtendimentos não configurada.");
}

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(conexaoHistorico));
builder.Services.AddSingleton<IHistoricoAtendimentos, HistoricoAtendimentosMongo>();
var conexaoCustos = builder.Configuration.GetConnectionString("CustosIa");
if (string.IsNullOrWhiteSpace(conexaoCustos))
{
    throw new InvalidOperationException("ConnectionStrings:CustosIa não configurada.");
}

builder.Services.AddSingleton<IRegistroConsumoTokens>(new RegistroConsumoTokensSqlServer(conexaoCustos));
builder.Services.AddSingleton<IOutboxMensagens>(new OutboxMensagensSqlServer(conexaoCustos));

// Publicação periódica da outbox na fila do RabbitMQ.
builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<OutboxOptions>()
    .Bind(builder.Configuration.GetSection(OutboxOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IPublicadorFila, PublicadorRabbitMq>();
builder.Services.AddSingleton<IProcessadorOutbox, ProcessadorOutbox>();
builder.Services.AddHostedService<PublicacaoOutboxHostedService>();

// Semantic Kernel com as funções que o agente pode executar (encaminhamento para o suporte humano).
builder.Services.AddKernel()
    .Plugins.AddFromType<EncaminhamentoPlugin>(EncaminhamentoPlugin.NomePlugin);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAtendimentoService, AtendimentoService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api.Atendimento v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
