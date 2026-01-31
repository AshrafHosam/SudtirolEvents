using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenDataHubAssistant.Core.Interfaces;
using OpenDataHubAssistant.Core.Services;
using OpenDataHubAssistant.Infrastructure.Configuration;
using OpenDataHubAssistant.Infrastructure.Data;
using OpenDataHubAssistant.Infrastructure.ExternalClients;
using OpenDataHubAssistant.Infrastructure.Repositories;
using OpenDataHubAssistant.Infrastructure.Services;
using Polly;
using Polly.Extensions.Http;

namespace OpenDataHubAssistant.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure settings
        services.Configure<OpenDataHubSettings>(
            configuration.GetSection(OpenDataHubSettings.SectionName));
        services.Configure<OpenAiSettings>(
            configuration.GetSection(OpenAiSettings.SectionName));

        // Get settings for HTTP client configuration
        var openDataHubSettings = configuration
            .GetSection(OpenDataHubSettings.SectionName)
            .Get<OpenDataHubSettings>() ?? new OpenDataHubSettings();

        // Configure DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=opendatahub.db";
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register repositories
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IWeatherSnapshotRepository, WeatherSnapshotRepository>();
        services.AddScoped<IRecommendationLogRepository, RecommendationLogRepository>();

        // Register core services
        services.AddSingleton<IWeatherInterpreterService, WeatherInterpreterService>();
        services.AddScoped<IActivityRecommendationService, ActivityRecommendationService>();

        // Configure retry policy for HTTP clients
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                openDataHubSettings.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        // Register HTTP clients with retry policy
        services.AddHttpClient<IOpenDataHubWeatherClient, OpenDataHubWeatherClient>(client =>
        {
            client.BaseAddress = new Uri(openDataHubSettings.WeatherApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(openDataHubSettings.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler(retryPolicy);

        services.AddHttpClient<IOpenDataHubEventsClient, OpenDataHubEventsClient>(client =>
        {
            client.BaseAddress = new Uri(openDataHubSettings.EventsApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(openDataHubSettings.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler(retryPolicy);

        services.AddHttpClient<IOpenDataHubPoiClient, OpenDataHubPoiClient>(client =>
        {
            client.BaseAddress = new Uri(openDataHubSettings.PoiApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(openDataHubSettings.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddPolicyHandler(retryPolicy);

        // Register OpenAI client
        var openAiSettings = configuration
            .GetSection(OpenAiSettings.SectionName)
            .Get<OpenAiSettings>() ?? new OpenAiSettings();

        services.AddHttpClient<ILlmService, OpenAiLlmService>(client =>
        {
            client.BaseAddress = new Uri(openAiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Add memory cache
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied
    /// </summary>
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Apply pending migrations
        await context.Database.MigrateAsync();
    }
}
