namespace LoreKeeper.Infrastructure.Configuration;

public class LoreOptions
{
    public string QdrantHost { get; set; } = "localhost";
    public int QdrantPort { get; set; } = 6334;
    public string CollectionName { get; set; } = "lore_test";
    public ulong VectorSize { get; set; } = 768;
    public string GeminiApiKey { get; set; } = string.Empty;
}