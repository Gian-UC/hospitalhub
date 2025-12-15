using Clinica.Api.Domain.Context;
using Clinica.Api.Messaging.Consumers;
using Clinica.Api.Services.Implementations;
using Clinica.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with MySQL
builder.Services.AddDbContext<ClinicaContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(
        cs,
        new MySqlServerVersion(new Version(8, 0, 44)),
        b => b.MigrationsAssembly("Clinica.Api")    
    );
});

// Register Services
builder.Services.AddScoped<IConsultaService, ConsultaService>();

// 🐇 RabbitMQ Consumer
builder.Services.AddHostedService<AgendamentoConfirmadoConsumer>();

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Migrations automáticas (bom pra dev e docker)
app.Lifetime.ApplicationStarted.Register(() =>
{
    Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClinicaContext>();

        var retries = 10;
        while (retries > 0)
        {
            try
            {
                await db.Database.MigrateAsync();
                break;
            }
            catch
            {
                retries--;
                await Task.Delay(5000);
            }
        }    
    });
});

// Swagger sempre habilitado (bom pra docker)
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();