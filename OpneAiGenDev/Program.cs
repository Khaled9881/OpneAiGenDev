using OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Text;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var apiKey = config["OpenAI:ApiKey"]!;
var client = new ChatClient("gpt-4o-mini", apiKey);

var messages = new List<ChatMessage>();

while (true)
{
    string prompt = Console.ReadLine();

    messages.Add(new UserChatMessage(prompt));

    var resp = client.CompleteChatStreamingAsync(messages);

    var fullResponse = new StringBuilder();

    await foreach (var item in resp)
    {
        foreach (var part in item.ContentUpdate)
        {
            Console.Write(part.Text);
            fullResponse.Append(part.Text);
        }
    }
    Console.WriteLine();

    messages.Add(new AssistantChatMessage(fullResponse.ToString()));
}