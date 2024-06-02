using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Postgres.DB;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Proiect Licenta API", Description = "Sound trek - Bucharest Noise Map with crowdsourcing", Version = "v1" });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Proiect Licenta V1");
    });
}

app.MapGet("/", () => DB.GetAllNoiseLevels());


app.Run();
