using LoreKeeper.Core.Interfaces;
using LoreKeeper.Core.Models;
using LoreKeeper.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace LoreKeeper.Infrastructure.Services;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly LoreOptions _options;

    public QdrantVectorStore(QdrantClient client, IOptions<LoreOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _client.CollectionExistsAsync(_options.CollectionName, cancellationToken);
        if (!exists)
        {
            await _client.CreateCollectionAsync(
                collectionName: _options.CollectionName,
                vectorsConfig: new VectorParams
                {
                    Size = _options.VectorSize,
                    Distance = Distance.Cosine
                },
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task UpsertChunkAsync(LoreChunk chunk, ReadOnlyMemory<float> vector, CancellationToken cancellationToken = default)
    {
        var point = new PointStruct
        {
            Id = chunk.Id,
            Vectors = vector.ToArray(),
            Payload =
            {
                ["text"] = chunk.Text,
                ["source_file"] = chunk.SourceFile,
                ["created_at"] = chunk.CreatedAtUtc.ToString("o")
            }
        };

        await _client.UpsertAsync(_options.CollectionName, new[] { point }, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LoreSearchResult>> SearchAsync(ReadOnlyMemory<float> queryVector, int limit = 3, CancellationToken cancellationToken = default)
    {
        var matches = await _client.SearchAsync(
            collectionName: _options.CollectionName,
            vector: queryVector.ToArray(),
            limit: (ulong)limit,
            cancellationToken: cancellationToken
        );

        return matches.Select(m => new LoreSearchResult(
            Text: m.Payload.TryGetValue("text", out var t) ? t.StringValue : string.Empty,
            SourceFile: m.Payload.TryGetValue("source_file", out var s) ? s.StringValue : string.Empty,
            Score: m.Score
        )).ToList();
    }
}