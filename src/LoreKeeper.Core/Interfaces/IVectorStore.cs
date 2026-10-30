using LoreKeeper.Core.Models;

namespace LoreKeeper.Core.Interfaces;

public interface IVectorStore
{
    Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);
    Task UpsertChunkAsync(LoreChunk chunk, ReadOnlyMemory<float> vector, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoreSearchResult>> SearchAsync(ReadOnlyMemory<float> queryVector, int limit = 3, CancellationToken cancellationToken = default);
}