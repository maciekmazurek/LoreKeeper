using LoreKeeper.Core.Interfaces;
using LoreKeeper.Infrastructure.Configuration;
using LoreKeeper.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;

namespace LoreKeeper.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLoreInfrastructure(
        this IServiceCollection services, 
        Action<LoreOptions> configureOptions)
    {
        var options = new LoreOptions();
        configureOptions(options);

        services.Configure(configureOptions);

        // 1. Register Qdrant client and vector store
        services.AddSingleton(new QdrantClient(options.QdrantHost, options.QdrantPort));
        services.AddScoped<IVectorStore, QdrantVectorStore>();

        // 2. Register Gemini Embedding through Semantic Kernel / Microsoft.Extensions.AI
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddGoogleAIEmbeddingGenerator(
            modelId: "gemini-embedding-001",
            apiKey: options.GeminiApiKey,
            dimensions: (int)options.VectorSize
        );
        var kernel = kernelBuilder.Build();

        services.AddSingleton(kernel.GetRequiredService<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>());
        services.AddScoped<ITextEmbeddingService, GeminiEmbeddingService>();

        // 3. Register facade service for lore operations
        services.AddScoped<ILoreService, LoreService>();

        return services;
    }
}