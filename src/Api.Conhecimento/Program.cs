using Api.Conhecimento.Configuracoes;
using Api.Conhecimento.Ingestao;
using Api.Conhecimento.Modelos;
using Api.Conhecimento.Rag;
using Api.Conhecimento.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Erros de validação no mesmo formato das demais respostas: { status, mensagem }.
        options.InvalidModelStateResponseFactory = context =>
        {
            var erros = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m));

            return new BadRequestObjectResult(
                new RespostaApi(StatusCodes.Status400BadRequest, string.Join(" ", erros)));
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Api.Conhecimento",
        Version = "v1",
        Description = "API de conhecimento: base de documentos corporativos para busca e RAG."
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services
    .AddOptions<RagOptions>()
    .Bind(builder.Configuration.GetSection(RagOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<OpenAIOptions>()
    .Bind(builder.Configuration.GetSection(OpenAIOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<IngestaoOptions>()
    .Bind(builder.Configuration.GetSection(IngestaoOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var conexaoRag = builder.Configuration.GetConnectionString("RagInfis");
if (string.IsNullOrWhiteSpace(conexaoRag))
{
    throw new InvalidOperationException("ConnectionStrings:RagInfis não configurada.");
}

builder.Services.AddNpgsqlDataSource(conexaoRag, dataSource => dataSource.UseVector());

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDivisorChunks, DivisorChunks>();
builder.Services.AddSingleton<IGeradorEmbeddings, OpenAIGeradorEmbeddings>();
builder.Services.AddSingleton<IBuscaConhecimento, BuscaConhecimento>();
builder.Services.AddSingleton<IExtratorTextoPdf, ExtratorTextoPdf>();
builder.Services.AddSingleton<IRepositorioChunks, RepositorioChunksPostgres>();
builder.Services.AddSingleton<IIngestaoDocumentos, IngestaoDocumentos>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<ConhecimentoTools>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api.Conhecimento v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapMcp("/mcp");

app.Run();

public partial class Program;
