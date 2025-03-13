using System.Text.Json;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.Data.Sqlite;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only

namespace GalahadAI.Memory;

public class Evolution
{
    private readonly LongTermMemory _longTermMemory;
    private readonly ITextEmbeddingGenerationService _embeddingGenerator;
    private readonly string _evolutionDbPath;
    private readonly TimeSpan _evolutionInterval = TimeSpan.FromDays(3); // Evolution every 3 days
    
    public Evolution(LongTermMemory longTermMemory, ITextEmbeddingGenerationService embeddingGenerator, string evolutionDbPath = "data/evolution.db")
    {
        _longTermMemory = longTermMemory;
        _embeddingGenerator = embeddingGenerator;
        _evolutionDbPath = evolutionDbPath;
        InitializeEvolutionDatabase();
    }

    private void InitializeEvolutionDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_evolutionDbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS evolution_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp DATETIME NOT NULL,
                summary TEXT NOT NULL,
                improvements TEXT NOT NULL,
                memory_state BLOB NOT NULL
            );";
        command.ExecuteNonQuery();
    }

    public async Task<bool> ShouldEvolveAsync()
    {
        using var connection = new SqliteConnection($"Data Source={_evolutionDbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MAX(timestamp) FROM evolution_history;";
        
        var lastEvolution = await command.ExecuteScalarAsync();
        if (lastEvolution == DBNull.Value) return true;
        
        var lastEvolutionStr = lastEvolution?.ToString();
        if (string.IsNullOrEmpty(lastEvolutionStr)) return true;
        
        var lastEvolutionTime = DateTime.Parse(lastEvolutionStr);
        return DateTime.UtcNow - lastEvolutionTime >= _evolutionInterval;
    }

    public async Task EvolveAsync(CancellationToken cancellationToken = default)
    {
        // 1. Analyze memory patterns
        var memoryAnalysis = await AnalyzeMemoryPatternsAsync(cancellationToken);
        
        // 2. Generate improvements
        var improvements = await GenerateImprovementsAsync(memoryAnalysis, cancellationToken);
        
        // 3. Summarize and clean memory
        var memorySummary = await SummarizeAndCleanMemoryAsync(cancellationToken);
        
        // 4. Store evolution record
        await StoreEvolutionRecordAsync(memorySummary, improvements, cancellationToken);
        
        // 5. Apply improvements
        await ApplyImprovementsAsync(improvements, cancellationToken);
    }

    private async Task<string> AnalyzeMemoryPatternsAsync(CancellationToken cancellationToken)
    {
        // Get all memories and analyze patterns
        var memories = await _longTermMemory.GetAllMemoriesAsync(cancellationToken);
        var patterns = new Dictionary<string, int>();
        
        foreach (var memory in memories)
        {
            // Analyze memory content and update patterns
            var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(memory.Content, null, cancellationToken);
            var similar = await _longTermMemory.SearchSimilarAsync(embedding, 5, cancellationToken);
            
            foreach (var match in similar)
            {
                var key = ExtractPattern(memory.Content, match.Content);
                if (!string.IsNullOrEmpty(key))
                {
                    patterns[key] = patterns.GetValueOrDefault(key, 0) + 1;
                }
            }
        }

        return JsonSerializer.Serialize(patterns);
    }

    private string ExtractPattern(string content1, string content2)
    {
        // Simple pattern extraction - can be enhanced with more sophisticated algorithms
        var words1 = content1.Split(' ');
        var words2 = content2.Split(' ');
        var commonWords = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase);
        return string.Join(" ", commonWords);
    }

    private async Task<string> GenerateImprovementsAsync(string memoryAnalysis, CancellationToken cancellationToken)
    {
        // TODO: Implement improvement generation based on memory analysis
        await Task.CompletedTask; // Placeholder for future implementation
        return JsonSerializer.Serialize(new List<string> { "No improvements needed at this time." });
    }

    private async Task<string> SummarizeAndCleanMemoryAsync(CancellationToken cancellationToken)
    {
        var memories = await _longTermMemory.GetAllMemoriesAsync(cancellationToken);
        var summaries = new List<string>();
        var currentBatch = new List<Memory>();

        // Group similar memories
        foreach (var memory in memories)
        {
            if (currentBatch.Count == 0)
            {
                currentBatch.Add(memory);
                continue;
            }

            var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(memory.Content, null, cancellationToken);
            var similarityScore = await GetMaxSimilarityAsync(embedding, currentBatch, cancellationToken);

            if (similarityScore > 0.8f) // High similarity threshold
            {
                currentBatch.Add(memory);
            }
            else
            {
                // Summarize current batch
                summaries.Add(await SummarizeBatchAsync(currentBatch, cancellationToken));
                currentBatch.Clear();
                currentBatch.Add(memory);
            }
        }

        if (currentBatch.Count > 0)
        {
            summaries.Add(await SummarizeBatchAsync(currentBatch, cancellationToken));
        }

        // Clean old memories
        await _longTermMemory.CleanupAsync(cancellationToken);

        return JsonSerializer.Serialize(summaries);
    }

    private Task<string> SummarizeBatchAsync(List<Memory> memories, CancellationToken cancellationToken)
    {
        // Combine similar memories into a concise summary
        var contents = memories.Select(m => m.Content);
        return Task.FromResult(string.Join(" | ", contents));
    }

    private async Task StoreEvolutionRecordAsync(string memorySummary, string improvements, CancellationToken cancellationToken)
    {
        using var connection = new SqliteConnection($"Data Source={_evolutionDbPath}");
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO evolution_history (timestamp, summary, improvements, memory_state)
            VALUES (@timestamp, @summary, @improvements, @memory_state);";

        command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
        command.Parameters.AddWithValue("@summary", memorySummary);
        command.Parameters.AddWithValue("@improvements", improvements);
        command.Parameters.AddWithValue("@memory_state", JsonSerializer.Serialize(await _longTermMemory.GetMemoryStatsAsync(cancellationToken)));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ApplyImprovementsAsync(string improvements, CancellationToken cancellationToken)
    {
        var improvementList = JsonSerializer.Deserialize<List<string>>(improvements);
        foreach (var improvement in improvementList!)
        {
            // Store improvement as a special memory for future reference
            await _longTermMemory.StoreAsync(new Memory
            {
                Key = $"evolution_{DateTime.UtcNow.Ticks}",
                Content = $"Applied improvement: {improvement}",
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30) // Keep evolution records for 30 days
            }, cancellationToken);
        }
    }

    private async Task<float> GetMaxSimilarityAsync(ReadOnlyMemory<float> embedding, IEnumerable<Memory> memories, CancellationToken cancellationToken = default)
    {
        float maxSimilarity = 0;
        foreach (var memory in memories)
        {
            var similarity = await Task.Run(() => _longTermMemory.GetMaxSimilarityAsync(embedding, new[] { memory }, cancellationToken).Result, cancellationToken);
            maxSimilarity = Math.Max(maxSimilarity, similarity);
        }
        return maxSimilarity;
    }
} 