using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only

namespace GalahadAI.Memory;

public class LongTermMemory : IDisposable
{
    private readonly string _connectionString;
    private readonly ITextEmbeddingGenerationService _embeddingGenerator;
    private bool _disposed;

    public LongTermMemory(ITextEmbeddingGenerationService embeddingGenerator, string dbPath = "data/long_term_memory.db")
    {
        _embeddingGenerator = embeddingGenerator;
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Create memories table
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS memories (
                key TEXT PRIMARY KEY,
                content TEXT NOT NULL,
                embedding BLOB NOT NULL,
                timestamp DATETIME NOT NULL,
                expires_at DATETIME
            );";
        command.ExecuteNonQuery();
    }

    public async Task StoreAsync(Memory memory, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT OR REPLACE INTO memories (key, content, embedding, timestamp, expires_at)
            VALUES (@key, @content, @embedding, @timestamp, @expires_at);";

        command.Parameters.AddWithValue("@key", memory.Key);
        command.Parameters.AddWithValue("@content", memory.Content);
        command.Parameters.AddWithValue("@embedding", JsonSerializer.Serialize(memory.Embedding.ToArray()));
        command.Parameters.AddWithValue("@timestamp", memory.Timestamp);
        command.Parameters.AddWithValue("@expires_at", memory.ExpiresAt as object ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<Memory>> SearchSimilarAsync(ReadOnlyMemory<float> queryEmbedding, int limit = 5, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get all non-expired memories
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT key, content, embedding, timestamp, expires_at
            FROM memories
            WHERE expires_at IS NULL OR expires_at > @now;";

        command.Parameters.AddWithValue("@now", DateTime.UtcNow);

        var memories = new List<Memory>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var memory = new Memory
            {
                Key = reader.GetString(0),
                Content = reader.GetString(1),
                Embedding = new ReadOnlyMemory<float>(JsonSerializer.Deserialize<float[]>(reader.GetString(2))!),
                Timestamp = reader.GetDateTime(3),
                ExpiresAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
            };
            memories.Add(memory);
        }

        // Calculate similarities and sort
        return memories
            .Select(m => (Memory: m, Similarity: CosineSimilarity(queryEmbedding, m.Embedding)))
            .OrderByDescending(x => x.Similarity)
            .Take(limit)
            .Select(x => x.Memory);
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

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM memories 
            WHERE expires_at IS NOT NULL AND expires_at <= @now;";

        command.Parameters.AddWithValue("@now", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<Memory>> GetAllMemoriesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT key, content, embedding, timestamp, expires_at FROM memories;";

        var memories = new List<Memory>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            memories.Add(new Memory
            {
                Key = reader.GetString(0),
                Content = reader.GetString(1),
                Embedding = new ReadOnlyMemory<float>(JsonSerializer.Deserialize<float[]>(reader.GetString(2))!),
                Timestamp = reader.GetDateTime(3),
                ExpiresAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
            });
        }

        return memories;
    }

    public async Task<float> GetMaxSimilarityAsync(ReadOnlyMemory<float> embedding, IEnumerable<Memory> memories, CancellationToken cancellationToken = default)
    {
        float maxSimilarity = 0;
        foreach (var memory in memories)
        {
            var similarity = CosineSimilarity(embedding, memory.Embedding);
            maxSimilarity = Math.Max(maxSimilarity, similarity);
        }
        return maxSimilarity;
    }

    public async Task<object> GetMemoryStatsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT 
                COUNT(*) as total_memories,
                COUNT(CASE WHEN expires_at IS NOT NULL THEN 1 END) as expiring_memories,
                MIN(CASE WHEN timestamp IS NOT NULL THEN timestamp END) as oldest_memory,
                MAX(CASE WHEN timestamp IS NOT NULL THEN timestamp END) as newest_memory
            FROM memories;";

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new
            {
                TotalMemories = reader.GetInt32(0),
                ExpiringMemories = reader.GetInt32(1),
                OldestMemory = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                NewestMemory = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3)
            };
        }

        return new { TotalMemories = 0, ExpiringMemories = 0 };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Cleanup if needed
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