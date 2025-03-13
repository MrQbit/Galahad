using System.Collections.Concurrent;
using Microsoft.SemanticKernel;

namespace GalahadAI.Memory;

public class ShortTermMemory
{
    private readonly ConcurrentDictionary<string, Memory> _memories;

    public ShortTermMemory()
    {
        _memories = new ConcurrentDictionary<string, Memory>();
    }

    public void Store(Memory memory)
    {
        _memories[memory.Key] = memory;
    }

    public IEnumerable<Memory> SearchSimilar(ReadOnlyMemory<float> queryEmbedding, int limit = 5)
    {
        return _memories.Values
            .Where(m => !IsExpired(m))
            .OrderByDescending(m => CosineSimilarity(queryEmbedding, m.Embedding))
            .Take(limit)
            .ToList();
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
        var expiredKeys = _memories.Where(kvp => IsExpired(kvp.Value))
                                 .Select(kvp => kvp.Key)
                                 .ToList();

        foreach (var key in expiredKeys)
        {
            _memories.TryRemove(key, out _);
        }
    }
} 