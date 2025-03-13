using System.Text.Json;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only

namespace GalahadAI.Memory;

public class WarmMemory : IDisposable
{
    private readonly string _basePath;
    private readonly ITextEmbeddingGenerationService _embeddingGenerator;
    private readonly Dictionary<string, Memory> _cache;
    private readonly object _lock = new();
    private bool _disposed;

    public WarmMemory(ITextEmbeddingGenerationService embeddingGenerator, string basePath = "data/warm_memory")
    {
        _embeddingGenerator = embeddingGenerator;
        _basePath = basePath;
        _cache = new Dictionary<string, Memory>();
        
        Directory.CreateDirectory(_basePath);
        LoadCache();
    }

    private void LoadCache()
    {
        lock (_lock)
        {
            foreach (var file in Directory.GetFiles(_basePath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var memory = JsonSerializer.Deserialize<Memory>(json);
                    if (memory != null && !IsExpired(memory))
                    {
                        _cache[memory.Key] = memory;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading memory file {file}: {ex.Message}");
                }
            }
        }
    }

    public Task StoreAsync(Memory memory, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, $"{memory.Key}.json");
        var json = JsonSerializer.Serialize(memory);
        
        lock (_lock)
        {
            _cache[memory.Key] = memory;
            File.WriteAllText(filePath, json);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<Memory>> SearchSimilarAsync(ReadOnlyMemory<float> queryEmbedding, int limit = 5, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var results = _cache.Values
                .Where(m => !IsExpired(m))
                .OrderByDescending(m => CosineSimilarity(queryEmbedding, m.Embedding))
                .Take(limit)
                .ToList();

            return Task.FromResult<IEnumerable<Memory>>(results);
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

    private static bool IsExpired(Memory memory)
    {
        return memory.ExpiresAt.HasValue && memory.ExpiresAt.Value < DateTime.UtcNow;
    }

    public void Cleanup()
    {
        lock (_lock)
        {
            var expiredKeys = _cache.Where(kvp => IsExpired(kvp.Value))
                                  .Select(kvp => kvp.Key)
                                  .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
                var filePath = Path.Combine(_basePath, $"{key}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Cleanup();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
} 