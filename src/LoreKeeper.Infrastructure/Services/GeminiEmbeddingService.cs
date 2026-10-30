using LoreKeeper.Core.Interfaces;
using Microsoft.Extensions.AI;

namespace LoreKeeper.Infrastructure.Services;

public class GeminiEmbeddingService : ITextEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public GeminiEmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _generator = generator;
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var result = await _generator.GenerateAsync([text], cancellationToken: cancellationToken);
        return result[0].Vector;
    }
}