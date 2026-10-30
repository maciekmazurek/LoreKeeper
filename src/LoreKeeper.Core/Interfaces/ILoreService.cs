using LoreKeeper.Core.Models;

namespace LoreKeeper.Core.Interfaces;

public interface ILoreService
{
    Task IndexChunkAsync(string text, string sourceFile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LoreSearchResult>> FindRelevantLoreAsync(string query, int limit = 3, CancellationToken cancellationToken = default);
}