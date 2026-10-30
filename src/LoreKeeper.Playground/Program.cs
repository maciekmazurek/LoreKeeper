using LoreKeeper.Core.Interfaces;
using LoreKeeper.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
    ?? throw new InvalidOperationException("Missing GEMINI_API_KEY environment variable.");

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((_, services) =>
    {
        // 1. Registration of the infrastructure (Qdrant, Embedding, LoreService)
        services.AddLoreInfrastructure(options =>
        {
            options.GeminiApiKey = apiKey;
            options.CollectionName = "lore_test";
            options.VectorSize = 768;
            options.QdrantHost = "localhost";
            options.QdrantPort = 6334;
        });

        // 2. Registration of the LLM chat service (Gemini)
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddGoogleAIGeminiChatCompletion(
            modelId: "gemini-3.6-flash",
            apiKey: apiKey
        );
        var kernel = kernelBuilder.Build();
        services.AddSingleton(kernel.GetRequiredService<IChatCompletionService>());
    })
    .Build();

using var scope = host.Services.CreateScope();

var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
var loreService = scope.ServiceProvider.GetRequiredService<ILoreService>();
var chatService = scope.ServiceProvider.GetRequiredService<IChatCompletionService>();

// 1. Ensure Qdrant collection exists
Console.WriteLine("--> Ensuring Qdrant collection exists...");
await vectorStore.EnsureCollectionExistsAsync();

// 2. Indexing lore (Writing to vector memory)
Console.WriteLine("--> Indexing lore chunk...");
await loreService.IndexChunkAsync(
    text: "Krasnoludy z Klanu Żelaza nie znoszą magii i używają wyłącznie run.",
    sourceFile: "dwarf_lore.md"
);

const string userQuery = "Czego używają krasnoludy zamiast czarów?";
Console.WriteLine($"\n[User Query]: \"{userQuery}\"");

// 3. RETRIEVAL (Searching for relevant lore in Qdrant)
Console.WriteLine("--> Searching relevant facts in Qdrant...");
var relevantChunks = await loreService.FindRelevantLoreAsync(userQuery, limit: 1);
var topMatch = relevantChunks.FirstOrDefault();

if (topMatch == null)
{
    Console.WriteLine("No relevant lore found in the database.");
    return;
}

Console.WriteLine($"--> Retrieved from [{topMatch.SourceFile}] (Score: {topMatch.Score:F4}): \"{topMatch.Text}\"");

// 5. AUGMENTED GENERATION (Using LLM to generate grounded answer)
var prompt = $"""
    Jesteś Strażnikiem Wiedzy (LoreKeeper). 
    Odpowiedz na pytanie użytkownika WYŁĄCZNIE na podstawie poniższego kontekstu z dokumentacji.
    Jeśli kontekst nie pozwala na odpowiedź, napisz wprost: "Moje kroniki o tym milczą."

    [KONTEKST]:
    {topMatch.Text}

    [PYTANIE]:
    {userQuery}
    """;

Console.WriteLine("\n--> Generating grounded answer via LLM...");
var response = await chatService.GetChatMessageContentAsync(prompt);

Console.WriteLine($"\n[LoreKeeper Answer]: {response.Content}");