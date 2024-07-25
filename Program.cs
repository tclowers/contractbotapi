using System.Net.Http;
using ContractBotApi.Data;
using Microsoft.EntityFrameworkCore;
using ContractBotApi.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add configuration sources
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient
builder.Services.AddHttpClient();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Add database context
var connectionString = builder.Configuration["CONTRACTBOT_DB_CONNECTION_STRING"];

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string not found. Please set the CONTRACTBOT_DB_CONNECTION_STRING environment variable.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register GPTController as a scoped service
builder.Services.AddScoped<GPTController>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var scope = app.Services.CreateScope();
            var gptController = scope.ServiceProvider.GetRequiredService<GPTController>();
            await gptController.HandleWebSocket(context); // Pass the HttpContext here
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}