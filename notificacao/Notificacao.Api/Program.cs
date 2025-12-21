using Notificacao.Api.Messaging;
using Notificacao.Api.Services.Email;
using Notificacao.Api.Services.Pacientes;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddHttpClient<IPacienteCliente, PacienteClient>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["AgendamentosApi:BaseUrl"]!);
});

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddHostedService<AgendamentoConfirmadoConsumer>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("Notificacao.Api rodando ✅"));

app.Run();