using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.AI;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Security.Cryptography;
using System.Text;

var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException(
        "The GEMINI_API_KEY environment variable is missing. Set it in the current PowerShell session before running the application.");
}

// 1. Registration of Chat + Embedding in Semantic Kernel
var builder = Kernel.CreateBuilder();

builder.AddGoogleAIGeminiChatCompletion(
    modelId: "gemini-3.6-flash",
    apiKey: apiKey
);

builder.AddGoogleAIEmbeddingGenerator(
    modelId: "gemini-embedding-001",
    apiKey: apiKey,
    dimensions: 768
);

var kernel = builder.Build();

// 2. Chat request to Gemini
var chatService = kernel.GetRequiredService<IChatCompletionService>();
Console.WriteLine("Sending a chat request to Gemini...");
var response = await chatService.GetChatMessageContentAsync("Wymyśl jednozdaniowy fakt o krasnoludach.");
Console.WriteLine($"Gemini response: {response}\n");

// 3. Connecting to Qdrant and creating a collection if it doesn't exist
var qdrantClient = new QdrantClient("localhost", 6334);
const string collectionName = "lore_test";
const ulong vectorSize = 768; // Output dimension configured for gemini-embedding-001

var collectionExists = await qdrantClient.CollectionExistsAsync(collectionName);
if (!collectionExists)
{
    Console.WriteLine($"Creating collection '{collectionName}' in Qdrant...");
    await qdrantClient.CreateCollectionAsync(
        collectionName: collectionName,
        vectorsConfig: new VectorParams
        {
            Size = vectorSize,
            Distance = Distance.Cosine
        }
    );
    Console.WriteLine("Collection created successfully.\n");
}
else
{
    Console.WriteLine($"Collection '{collectionName}' already exists.\n");
}

// 3. Generating an embedding for a text chunk and saving it to Qdrant
var embeddingGenerator = kernel.GetRequiredService<
    IEmbeddingGenerator<string, Embedding<float>>>();

const string loreChunkText = "Krasnoludy z Klanu Żelaza nie znoszą magii i używają wyłącznie run.";

Console.WriteLine($"Generating embedding for: \"{loreChunkText}\"...");

var embeddings = await embeddingGenerator.GenerateAsync([loreChunkText]);
var embedding = embeddings[0].Vector;

Console.WriteLine($"Embedding generated! Vector size: {embedding.Length}");

// Creating a point for Qdrant (ID, Vectors, Payload)
var hash = MD5.HashData(Encoding.UTF8.GetBytes("manual_chunk_01.md"));
var pointId = new Guid(hash);

var point = new PointStruct
{
    Id = pointId,
    Vectors = embedding.ToArray(),
    Payload =
    {
        ["text"] = loreChunkText,
        ["source_file"] = "manual_chunk_01.md",
        ["created_at"] = DateTime.UtcNow.ToString("o")
    }
};

Console.WriteLine("Writing point to Qdrant...");
await qdrantClient.UpsertAsync(collectionName, new[] { point });

Console.WriteLine($"Point of ID {pointId} has been successfully written to collection '{collectionName}'!");

const string userQuery = "Czego używają kowale zamiast czarów?";
Console.WriteLine($"Query: \"{userQuery}\"");

Console.WriteLine("Generating embedding for the query...");
var queryEmbeddings = await embeddingGenerator.GenerateAsync([userQuery]);
var queryEmbedding = queryEmbeddings[0].Vector;

Console.WriteLine("Searching for the most similar point in Qdrant...");
var searchResults = await qdrantClient.SearchAsync(
    collectionName: collectionName,
    vector: queryEmbedding.ToArray(),
    limit: 1 // Fetch only the single closest match
);

var topMatch = searchResults.FirstOrDefault();

if (topMatch != null)
{
    Console.WriteLine($"\nMatch found! Score: {topMatch.Score:F4}");
    
    if (topMatch.Payload.TryGetValue("text", out var textValue))
    {
        Console.WriteLine($"Retrieved content: \"{textValue.StringValue}\"");
    }

    if (topMatch.Payload.TryGetValue("source_file", out var sourceValue))
    {
        Console.WriteLine($"Source file: {sourceValue.StringValue}");
    }
}
else
{
    Console.WriteLine("No matches found.");
}