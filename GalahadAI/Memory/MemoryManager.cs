using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only

namespace GalahadAI.Memory;

public class MemoryManager : IDisposable
{
    private readonly ITextEmbeddingGenerationService _embeddingGenerator;
    private readonly ShortTermMemory _shortTermMemory;
    private readonly WarmMemory _warmMemory;
    private bool _disposed;

    public MemoryManager(ITextEmbeddingGenerationService embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
        _shortTermMemory = new ShortTermMemory();
        _warmMemory = new WarmMemory(embeddingGenerator);
    }

    public async Task<bool> StoreMemoryAsync(string key, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(content, null, cancellationToken);
            var memory = new Memory
            {
                Key = key,
                Content = content,
                Embedding = embedding,
                Timestamp = DateTime.UtcNow
            };

            _shortTermMemory.Store(memory);
            await _warmMemory.StoreAsync(memory, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error storing memory: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<Memory>> SearchSimilarAsync(string query, int limit = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query, null, cancellationToken);
            
            var shortTermResults = _shortTermMemory.SearchSimilar(queryEmbedding, limit);
            var warmResults = await _warmMemory.SearchSimilarAsync(queryEmbedding, limit, cancellationToken);
            
            return shortTermResults.Concat(warmResults)
                .OrderByDescending(m => CosineSimilarity(queryEmbedding, m.Embedding))
                .Take(limit);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching similar memories: {ex.Message}");
            return Array.Empty<Memory>();
        }
    }

    private static float CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var aSpan = a.Span;
        var bSpan = b.Span;
        
        if (aSpan.Length != bSpan.Length) return 0;
        
        float dotProduct = 0, aMagnitude = 0, bMagnitude = 0;
        
        for (int i = 0; i < aSpan.Length; i++)
        {
            dotProduct += aSpan[i] * bSpan[i];
            aMagnitude += aSpan[i] * aSpan[i];
            bMagnitude += bSpan[i] * bSpan[i];
        }
        
        float magnitude = MathF.Sqrt(aMagnitude) * MathF.Sqrt(bMagnitude);
        return magnitude == 0 ? 0 : dotProduct / magnitude;
    }

    public void Cleanup()
    {
        _shortTermMemory.Cleanup();
        _warmMemory.Cleanup();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _warmMemory.Dispose();
            _disposed = true;
        }
    }
} 