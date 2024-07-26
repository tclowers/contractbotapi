using System.Net.Http;
using ContractBotApi.Data;
using Microsoft.EntityFrameworkCore;
using Azure.Storage.Blobs;

// Register the Code Pages Encoding Provider
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

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

// Add Azure Blob Storage
var azureStorageConnectionString = builder.Configuration["AZURE_BLOB_STORAGE_CONNECTION_STRING"];
if (string.IsNullOrEmpty(azureStorageConnectionString))
{
    throw new InvalidOperationException("Azure Blob Storage connection string not found. Please set the AZURE_BLOB_STORAGE_CONNECTION_STRING environment variable.");
}
Console.WriteLine($"Azure Storage connection string: {azureStorageConnectionString?.Substring(0, Math.Min(azureStorageConnectionString.Length, 20))}...");
builder.Services.AddSingleton(x => new BlobServiceClient(azureStorageConnectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
