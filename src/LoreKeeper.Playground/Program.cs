using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

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