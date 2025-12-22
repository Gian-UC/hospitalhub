using Clinica.Api.Domain.Context;
using Clinica.Api.Messaging.Consumers;
using Clinica.Api.Services.Implementations;
using Clinica.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
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


builder.Services.AddDbContext<ClinicaContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(
        cs,
        new MySqlServerVersion(new Version(8, 0, 44)),
        b => b.MigrationsAssembly("Clinica.Api")
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
            ValidIssuers = validIssuers,

            ValidateAudience = true,
            ValidAudiences = new[] { keycloakAudience!, "account" },

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
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
    options.AddPolicy("MedicoOnly", policy => policy.RequireRole("MEDICO", "ADMIN"));

    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "Clinica.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });


builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IDoencaService, DoencaService>();
builder.Services.AddScoped<ISintomaService, SintomaService>();


builder.Services.AddHostedService<AgendamentoConfirmadoConsumer>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Clinica API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();