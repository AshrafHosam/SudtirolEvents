using OpenDataHubAssistant.Infrastructure;
using OpenDataHubAssistant.Infrastructure.Configuration;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from environment variables for sensitive data
// TODO: Set these environment variables before running:
// - OPENAI_API_KEY: Your OpenAI API key
builder.Configuration.AddEnvironmentVariables();

// Override OpenAI API key from environment if set
var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (!string.IsNullOrEmpty(openAiApiKey))
{
    builder.Configuration[$"{OpenAiSettings.SectionName}:ApiKey"] = openAiApiKey;
}

// Add controllers with JSON options to serialize enums as strings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Open Data Hub Weather & Activity Assistant API",
        Version = "v1",
        Description = "API for weather data, activity recommendations, and AI-powered chat for South Tyrol"
    });
});

// Add Infrastructure services (repositories, external clients, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Initialize database
await DependencyInjection.InitializeDatabaseAsync(app.Services);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Open Data Hub Assistant API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

// Add a simple root endpoint
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
