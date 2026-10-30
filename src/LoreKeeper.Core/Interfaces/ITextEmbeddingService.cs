namespace LoreKeeper.Core.Interfaces;

public interface ITextEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}