namespace LoreKeeper.Core.Models;

public record LoreSearchResult(
    string Text,
    string SourceFile,
    float Score
);