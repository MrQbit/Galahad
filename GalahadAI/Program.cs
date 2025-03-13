using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using LancelotAI.Memory;

// Create HTTP clients
var lmStudioClient = new HttpClient { BaseAddress = new Uri("http://localhost:1234/v1/") };
lmStudioClient.Timeout = TimeSpan.FromMinutes(5); // Increase timeout for LM Studio

var mcpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434/api/") }; // Ollama API endpoint
mcpClient.Timeout = TimeSpan.FromMinutes(5); // Set reasonable timeout for Ollama

// Initialize memory systems
var embeddingGenerator = new OllamaEmbeddingGenerator("http://localhost:11434");
var longTermMemory = new LongTermMemory(embeddingGenerator);

// Test prompts
var testPrompts = new[]
{
    "Write a haiku about coding",
    "Explain what is semantic kernel in one sentence",
    "What's your favorite programming language and why?"
};

Console.WriteLine("=== Testing LM Studio and Ollama Integration ===\n");

foreach (var prompt in testPrompts)
{
    Console.WriteLine($"Prompt: {prompt}\n");
    
    // Test LM Studio
    Console.WriteLine("LM Studio Response:");
    var lmResponse = await GetCompletionAsync(prompt);
    Console.WriteLine($"{lmResponse}\n");
    
    // Test Ollama (non-streaming)
    Console.WriteLine("Ollama Response (non-streaming):");
    var ollamaResponse = await GetMcpCompletionAsync(prompt);
    Console.WriteLine($"{ollamaResponse}\n");
    
    // Test Ollama (streaming)
    Console.WriteLine("Ollama Response (streaming):");
    await foreach (var chunk in GetStreamingMcpCompletionAsync(prompt))
    {
        Console.Write(chunk);
    }
    Console.WriteLine("\n");
    
    Console.WriteLine("----------------------------------------\n");
}

// Test LM Studio responses
Console.WriteLine("Testing LM Studio responses:");
Console.WriteLine("Non-streaming response:");
var response = await GetCompletionAsync("What is the meaning of life?");
Console.WriteLine($"AI Response: {response}\n");

Console.WriteLine("Streaming response:");
await foreach (var chunk in GetStreamingCompletionAsync("What is the meaning of life?"))
{
    Console.Write(chunk);
}
Console.WriteLine("\n");

// Test MCP completion
Console.WriteLine("Testing MCP (Ollama) response:");
var mcpResponse = await GetMcpCompletionAsync("What is the meaning of life?");
Console.WriteLine($"MCP Response: {mcpResponse}\n");

// Run memory system tests
Console.WriteLine("\n=== Testing Memory System ===\n");
await MemoryTests.RunTests();

// Initialize and check evolution system
var evolution = new Evolution(longTermMemory, embeddingGenerator);
if (await evolution.ShouldEvolveAsync())
{
    Console.WriteLine("\n=== Starting Evolution Process ===\n");
    await evolution.EvolveAsync();
    Console.WriteLine("Evolution completed successfully!");
}

// Helper method to get completions from LM Studio
async Task<string> GetCompletionAsync(string prompt)
{
    try
    {
        var requestBody = new CompletionRequest
        {
            Model = "local-model",
            Prompt = prompt,
            MaxTokens = 2000,
            Stream = false
        };
        
        using var response = await lmStudioClient.PostAsJsonAsync("completions", requestBody);
        
        // Log the response status and content for debugging
        Console.WriteLine($"LM Studio Status Code: {response.StatusCode}");
        var rawContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw Response: {rawContent}");
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<CompletionResponse>();
        return result?.Choices?[0]?.Text ?? "No response received";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message}\nStack Trace: {ex.StackTrace}";
    }
}

// Helper method to get streaming completions from LM Studio
async IAsyncEnumerable<string> GetStreamingCompletionAsync(string prompt)
{
    var requestBody = new CompletionRequest
    {
        Model = "local-model",
        Prompt = prompt,
        MaxTokens = 2000,
        Stream = true
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "completions")
    {
        Content = JsonContent.Create(requestBody)
    };

    using var response = await lmStudioClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();

    using var stream = await response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    var chunks = new List<string>();
    
    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();
        if (string.IsNullOrEmpty(line)) continue;
        if (!line.StartsWith("data: ")) continue;
        if (line == "data: [DONE]") break;

        try
        {
            var data = line.Substring(6);
            var streamingResponse = JsonSerializer.Deserialize<CompletionResponse>(data);
            var text = streamingResponse?.Choices?[0]?.Text ?? "";
            if (!string.IsNullOrEmpty(text))
            {
                chunks.Add(text);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing chunk: {ex.Message}");
        }
    }

    foreach (var chunk in chunks)
    {
        yield return chunk;
    }
}

// Helper method for MCP completions
async Task<string> GetMcpCompletionAsync(string prompt)
{
    try
    {
        var requestBody = new McpCompletionRequest
        {
            Model = "llama3.2:latest",
            Prompt = prompt,
            Stream = false
        };
        
        using var response = await mcpClient.PostAsJsonAsync("generate", requestBody);
        
        // Log the response status and content for debugging
        Console.WriteLine($"Ollama Status Code: {response.StatusCode}");
        var rawContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Raw Response: {rawContent}");
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<McpCompletionResponse>();
        return result?.Response ?? "No response received";
    }
    catch (Exception ex)
    {
        return $"MCP Error: {ex.Message}\nStack Trace: {ex.StackTrace}";
    }
}

// Helper method for MCP streaming completions
async IAsyncEnumerable<string> GetStreamingMcpCompletionAsync(string prompt)
{
    var requestBody = new McpCompletionRequest
    {
        Model = "llama3.2:latest",
        Prompt = prompt,
        Stream = true
    };

    var request = new HttpRequestMessage(HttpMethod.Post, "generate")
    {
        Content = JsonContent.Create(requestBody)
    };

    using var response = await mcpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();

    using var stream = await response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream)
    {
        string? line = null;
        McpCompletionResponse? streamingResponse = null;

        try
        {
            line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;

            streamingResponse = JsonSerializer.Deserialize<McpCompletionResponse>(line);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error processing chunk: {ex.Message}");
            continue;
        }

        if (streamingResponse?.Response != null)
        {
            yield return streamingResponse.Response;
        }
        
        if (streamingResponse?.Done == true)
        {
            break;
        }
    }
}

// Models for JSON serialization
public class CompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";
    
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = "";
    
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

public class CompletionResponse
{
    [JsonPropertyName("choices")]
    public Choice[]? Choices { get; set; }
}

public class Choice
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

// Additional MCP models
public class McpCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "";
    
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = "";

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

public class McpCompletionResponse
{
    [JsonPropertyName("response")]
    public string? Response { get; set; }
    
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}
