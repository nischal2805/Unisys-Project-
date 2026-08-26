using ArazzoWorkflowPlatform.Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Database Context with SQLite
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=arazzo_platform.db";
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        // Add Repositories
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IInteractionRepository, InteractionRepository>();

        // Add RAG Services for handling large OpenAPI specs
        services.AddSingleton<IChunkingService, OpenApiChunkingService>();
        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();
        services.AddSingleton<IVectorStore, QdrantVectorStore>();

        // Add Semantic Kernel with Phi3 (Local Ollama)
        var phiEndpoint = configuration["Phi:Endpoint"];
        var phiModel = configuration["Phi:ModelName"] ?? "phi3:latest";

        if (!string.IsNullOrEmpty(phiEndpoint))
        {
            services.AddSingleton<Kernel>(sp =>
            {
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("SemanticKernel");
                
                var builder = Kernel.CreateBuilder();
                
                // Configure Ollama endpoint for Phi3
                // Ollama provides OpenAI-compatible API at /v1 endpoint
                var ollamaEndpoint = phiEndpoint.TrimEnd('/') + "/v1";
                
                logger?.LogInformation("[CONFIG] Configuring Semantic Kernel with Ollama");
                logger?.LogInformation("[CONFIG] Endpoint: {Endpoint}", ollamaEndpoint);
                logger?.LogInformation("[CONFIG] Model: {Model}", phiModel);
                
                builder.AddOpenAIChatCompletion(
                    modelId: phiModel,
                    apiKey: "not-needed", // Ollama doesn't validate API key but SK requires non-null
                    endpoint: new Uri(ollamaEndpoint)
                );
                
                return builder.Build();
            });
        }
        else
        {
            // Log warning at startup via console since DI isn't fully built yet
            Console.WriteLine("[CONFIG] WARNING: Phi endpoint not configured - LLM features will be disabled");
        }

        // Configure Qdrant connection
        var qdrantHost = configuration["Qdrant:Host"] ?? "localhost";
        var qdrantPort = configuration["Qdrant:Port"] ?? "6334";
        Console.WriteLine($"[CONFIG] Qdrant configured: {qdrantHost}:{qdrantPort}");

        return services;
    }
}
