using Agendamentos.Api.Domain.Context;
using Agendamentos.Api.Services.Implementations;
using Agendamentos.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HospitalAgendamentosContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(
        cs,
        new MySqlServerVersion(new Version(8, 0, 44)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
        }
    );
});

builder.Services.AddScoped<IAgendamentoService, AgendamentoService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalAgendamentosContext>();

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

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
