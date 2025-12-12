using Cirurgico.Api.Domain.Context;
using Cirurgico.Api.Messaging.Consumers;
using Cirurgico.Api.Services.Implementations;
using Cirurgico.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CirurgicoContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(
        cs,
        new MySqlServerVersion(new Version(8, 0, 44)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure();
        }
    );
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// RabbitMQ Consumer
builder.Services.AddHostedService<AgendamentoConfirmadoConsumer>();
builder.Services.AddScoped<ICirurgiaService, CirurgiaService>();

var app = builder.Build();
// MIGRATIONS AUTOMÁTICAS NO CIRÚRGICO 🎉
app.Lifetime.ApplicationStarted.Register(() =>
{
    Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CirurgicoContext>();

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
