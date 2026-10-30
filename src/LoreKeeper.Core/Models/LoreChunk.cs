namespace LoreKeeper.Core.Models;

public record LoreChunk(
    Guid Id,
    string Text,
    string SourceFile,
    DateTime CreatedAtUtc
);