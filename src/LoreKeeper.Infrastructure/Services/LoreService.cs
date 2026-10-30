using System.Security.Cryptography;
using System.Text;
using LoreKeeper.Core.Interfaces;
using LoreKeeper.Core.Models;

namespace LoreKeeper.Infrastructure.Services;

public class LoreService : ILoreService
{
    private readonly IVectorStore _vectorStore;
    private readonly ITextEmbeddingService _embeddingService;

    public LoreService(IVectorStore vectorStore, ITextEmbeddingService embeddingService)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
    }

    public async Task IndexChunkAsync(string text, string sourceFile, CancellationToken cancellationToken = default)
    {
        var vector = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken);
        
        // Deterministically generate a Guid based on the text and source file to ensure uniqueness
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"{sourceFile}:{text}"));
        var chunkId = new Guid(hash);

        var chunk = new LoreChunk(chunkId, text, sourceFile, DateTime.UtcNow);
        await _vectorStore.UpsertChunkAsync(chunk, vector, cancellationToken);
    }

    public async Task<IReadOnlyList<LoreSearchResult>> FindRelevantLoreAsync(string query, int limit = 3, CancellationToken cancellationToken = default)
    {
        var queryVector = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        return await _vectorStore.SearchAsync(queryVector, limit, cancellationToken);
    }
}