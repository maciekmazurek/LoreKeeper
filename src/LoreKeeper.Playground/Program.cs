using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Qdrant.Client;
using Qdrant.Client.Grpc;

var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException(
        "The GEMINI_API_KEY environment variable is missing. Run the application with the LoreKeeper.Playground profile.");
}

var builder = Kernel.CreateBuilder();
builder.AddGoogleAIGeminiChatCompletion(modelId: "gemini-3.5-flash", apiKey: apiKey);

var kernel = builder.Build();

var chatService = kernel.GetRequiredService<IChatCompletionService>();

Console.WriteLine("Sending a chat request to Gemini...");
var response = await chatService.GetChatMessageContentAsync("Wymyśl jednozdaniowy fakt o krasnoludach.");

Console.WriteLine($"Gemini response: {response}");

var qdrantClient = new QdrantClient("localhost", 6334);

const string collectionName = "lore_test";
const ulong vectorSize = 768; // For model text-embedding-004

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
    Console.WriteLine("Collection created successfully.");
}
else
{
    Console.WriteLine($"Collection '{collectionName}' already exists.");
}