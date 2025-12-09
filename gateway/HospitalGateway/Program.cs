var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger para o Gateway também
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
