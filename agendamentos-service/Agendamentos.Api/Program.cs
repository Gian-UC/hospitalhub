using Agendamentos.Api.Domain.Context;
using Agendamentos.Api.Messaging.Producer;
using Agendamentos.Api.Services.Implementations;
using Agendamentos.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);


var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
var keycloakAudience = builder.Configuration["Keycloak:Audience"];
var validIssuers = new[]
{
    "http://keycloak:8080/realms/hospital",
    "http://localhost:8085/realms/hospital"
};


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


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.Audience = keycloakAudience;
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuers = validIssuers,
            ValidAudiences = new[] { keycloakAudience!, "account" },
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                if (identity == null) return Task.CompletedTask;

                var realmAccess = context.Principal?.FindFirst("realm_access")?.Value;
                if (string.IsNullOrWhiteSpace(realmAccess)) return Task.CompletedTask;

                using var doc = JsonDocument.Parse(realmAccess);
                if (!doc.RootElement.TryGetProperty("roles", out var roles)) return Task.CompletedTask;

                foreach (var role in roles.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (!string.IsNullOrWhiteSpace(roleName))                    
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));                    
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));

    options.AddPolicy("UserOnly", policy => policy.RequireRole("USER"));
});

var redisConfiguration = builder.Configuration["Redis:Configuration"];
if (!string.IsNullOrWhiteSpace(redisConfiguration))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConfiguration;
        options.InstanceName = "agendamentos:";
    });
}

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "Agendamentos.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });

builder.Services.AddScoped<IAgendamentoService, AgendamentoService>();
builder.Services.AddSingleton<IAgendamentoConfirmadoProducer, AgendamentoConfirmadoProducer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Agendamentos API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT no formato: Bearer {seu token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

        _ = app.Services.GetRequiredService<AgendamentoConfirmadoProducer>();
    });
});


app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();