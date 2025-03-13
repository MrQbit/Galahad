using System.Text.Json;

namespace GalahadAI.Memory;

public interface IMemoryStore
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value, TimeSpan? ttl = null);
    Task<bool> DeleteAsync(string key);
    Task<IEnumerable<string>> SearchAsync(string query, int limit = 5);
    Task<bool> ExistsAsync(string key);
}

public record MemoryRecord
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; init; }
    public float[] Embedding { get; init; } = Array.Empty<float>();

    public string ToJson() => JsonSerializer.Serialize(this);
    
    public static MemoryRecord? FromJson(string json) => 
        JsonSerializer.Deserialize<MemoryRecord>(json);
} 