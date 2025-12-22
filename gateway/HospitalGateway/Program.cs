using Microsoft.AspNetCore.Authentication.JwtBearer;
using HospitalGateway.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
var keycloakAudience = builder.Configuration["Keycloak:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.Audience = keycloakAudience;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = true,
            ValidAudiences = new[] { keycloakAudience!, "account" },
            ValidateLifetime = true
        };
    });

// Fallback local (quando Redis não estiver configurado)
builder.Services.AddDistributedMemoryCache();

var redisConfiguration = builder.Configuration["Redis:Configuration"];
if (!string.IsNullOrWhiteSpace(redisConfiguration))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConfiguration;
        options.InstanceName = "gateway:";
    });
}

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: builder.Configuration["OTEL_SERVICE_NAME"] ?? "HospitalGateway"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });

builder.Services.AddOcelot(builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<IdempotencyMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

await app.UseOcelot();

app.MapControllers();

app.Run();
