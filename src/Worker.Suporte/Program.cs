using Worker.Suporte;
using Worker.Suporte.Configuracoes;
using Worker.Suporte.Email;
using Worker.Suporte.Inbox;
using Worker.Suporte.Processamento;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var conexaoInbox = builder.Configuration.GetConnectionString("CustosIa");
if (string.IsNullOrWhiteSpace(conexaoInbox))
{
    throw new InvalidOperationException("ConnectionStrings:CustosIa não configurada.");
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IInboxMensagens>(new InboxMensagensSqlServer(conexaoInbox));
builder.Services.AddSingleton<IEnviadorEmail, EnviadorEmailSmtp>();
builder.Services.AddSingleton<IProcessadorMensagemSuporte, ProcessadorMensagemSuporte>();
builder.Services.AddHostedService<ConsumidorFilaAtendimentos>();

var host = builder.Build();
host.Run();
