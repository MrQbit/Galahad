using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only

namespace GalahadAI.Memory;

public class OllamaEmbeddingGenerator : ITextEmbeddingGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OllamaEmbeddingGenerator(string baseUrl = "http://localhost:11434", string model = "llama3.2:latest")
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _model = model;
    }

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
    {
        { "model", _model },
        { "baseUrl", _httpClient.BaseAddress?.ToString() }
    };

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> texts, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        var embeddings = new List<ReadOnlyMemory<float>>();
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, kernel, cancellationToken);
            embeddings.Add(embedding);
        }
        return embeddings;
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new EmbeddingRequest { Model = _model, Prompt = text };
            var response = await _httpClient.PostAsJsonAsync("api/embeddings", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken);
            return new ReadOnlyMemory<float>(result?.Embedding ?? Array.Empty<float>());
        }
        catch (Exception)
        {
            // Return zero vector as fallback
            return new ReadOnlyMemory<float>(new float[4096]);
        }
    }

    private class EmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
} 