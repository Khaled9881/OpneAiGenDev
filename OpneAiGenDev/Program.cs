using OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Text;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var apiKey = config["OpenAI:ApiKey"]!;
var weatherApiKey = config["OpenWeatherMap:ApiKey"];


var client = new ChatClient("gpt-4o-mini", apiKey);

var messages = new List<ChatMessage>();


var getWeatherToll = ChatTool.CreateFunctionTool(

   functionName: "get_weather",
   functionDescription: "Gets the current weather for a city",
   functionParameters: BinaryData.FromString("""
       {
           "type": "object",
           "properties": {
               "city": {
                   "type": "string",
                   "description": "The city name, e.g. Paris"
               }
           },
           "required": ["city"]
       }
       """)
    );


var getTimeTool = ChatTool.CreateFunctionTool(
    functionName: "get_time",
    functionDescription: "Gets the current local time for a city",
    functionParameters: BinaryData.FromString("""
    {
        "type": "object",
        "properties": {
            "city": {
                "type": "string",
                "description": "The city name, e.g. Paris"
            }
        },
        "required": ["city"]
    }
    """)
);


var options = new ChatCompletionOptions
{
    Tools = { getWeatherToll, getTimeTool }
};


static async Task<string> GetWeather(string city, string apiKey)
{
    using var http = new HttpClient();

    var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

    var response = await http.GetStringAsync(url);
    var json = JsonDocument.Parse(response);

    var temp = json.RootElement.GetProperty("main").GetProperty("temp").GetDouble();
    var description = json.RootElement.GetProperty("weather")[0].GetProperty("description").GetString();
    var humidity = json.RootElement.GetProperty("main").GetProperty("humidity").GetInt32();

    return $"{temp}°C, {description}, humidity {humidity}%";
}


//static string GetWeather(string city, )
//{
//    return city.ToLower() switch
//    {
//        "paris" => "22°C, Sunny",
//        "london" => "15°C, Cloudy",
//        "cairo" => "35°C, Hot",
//        _ => "20°C, Clear"
//    };
//}

static string GetTime(string city)
{
    return city.ToLower() switch
    {
        "paris" => "3:45 PM",
        "london" => "2:45 PM",
        "cairo" => "4:45 PM",
        _ => "12:00 PM"
    };
}

messages.Add(new SystemChatMessage("""
    You are a helpful travel assistant.
    You only answer questions related to travel, weather, and time zones.
    If asked anything else, politely decline.
    Always be concise and friendly.
"""));




while (true) // outer loop — one conversation turn per iteration
{
    Console.Write("You: ");
    string prompt = Console.ReadLine()!;
    messages.Add(new UserChatMessage(prompt));

    while (true) // inner loop — keeps going until LLM has final answer
    {
        var response = await client.CompleteChatAsync(messages, options);
        messages.Add(new AssistantChatMessage(response));

        if (response.Value.FinishReason == ChatFinishReason.Stop)
        {
            Console.WriteLine("AI: " + response.Value.Content[0].Text);
            break; // ✅ exits inner loop, goes back to outer to read next input
        }

        if (response.Value.FinishReason == ChatFinishReason.ToolCalls)
        {
            foreach (var toolCall in response.Value.ToolCalls)
            {
                Console.WriteLine($"[Calling tool: {toolCall.FunctionName}({toolCall.FunctionArguments})]");

                string result = "";

                if (toolCall.FunctionName == "get_weather")
                {
                    var args2 = JsonDocument.Parse(toolCall.FunctionArguments);
                    var city = args2.RootElement.GetProperty("city").GetString()!;
                    result = await GetWeather(city, weatherApiKey);
                }
                else if (toolCall.FunctionName == "get_time")
                {
                    var args2 = JsonDocument.Parse(toolCall.FunctionArguments);
                    var city = args2.RootElement.GetProperty("city").GetString()!;
                    result = GetTime(city);
                }

                messages.Add(new ToolChatMessage(toolCall.Id, result));
                // no break — inner loop runs again so LLM sees the result
            }
        }
    }
}



//while (true)
//{
//    string prompt = Console.ReadLine();

//    messages.Add(new UserChatMessage(prompt));

//    var response = await client.CompleteChatAsync(messages, options);
//    messages.Add(new AssistantChatMessage(response));

//    // LLM is done — print final answer and break inner loop
//    if (response.Value.FinishReason == ChatFinishReason.Stop)
//    {
//        Console.WriteLine("AI: " + response.Value.Content[0].Text);
//        //break;
//    }

//    // LLM wants to call a tool
//    if (response.Value.FinishReason == ChatFinishReason.ToolCalls)
//    {
//        foreach (var toolCall in response.Value.ToolCalls)
//        {
//            Console.WriteLine($"[Calling tool: {toolCall.FunctionName}({toolCall.FunctionArguments})]");

//            string result = "";

//            if (toolCall.FunctionName == "get_weather")
//            {
//                var args2 = JsonDocument.Parse(toolCall.FunctionArguments);
//                var city = args2.RootElement.GetProperty("city").GetString()!;
//                result = GetWeather(city);
//            }

//            // Feed tool result back to LLM
//            messages.Add(new ToolChatMessage(toolCall.Id, result));
//        }
//    }




//var resp = client.CompleteChatStreamingAsync(messages, options);

//var fullResponse = new StringBuilder();

//await foreach (var item in resp)
//{
//    foreach (var part in item.ContentUpdate)
//    {
//        Console.Write(part.Text);
//        fullResponse.Append(part.Text);
//    }
//}
//Console.WriteLine();

//messages.Add(new AssistantChatMessage(fullResponse.ToString()));
