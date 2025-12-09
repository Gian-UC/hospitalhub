var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger correto pro .NET 8
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger sempre habilitado (bom pra docker)
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
