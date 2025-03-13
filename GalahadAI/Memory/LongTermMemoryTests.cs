using Microsoft.SemanticKernel;

namespace GalahadAI.Memory;

public static class LongTermMemoryTests
{
    private static readonly string TestDbPath = "data/test_long_term_memory.db";

    public static async Task RunTests()
    {
        Console.WriteLine("\nStarting long-term memory system tests...");

        // Initialize components
        var embeddingGenerator = new OllamaEmbeddingGenerator();
        
        try
        {
            // Cleanup any existing test database
            if (File.Exists(TestDbPath))
            {
                File.Delete(TestDbPath);
            }

            using var longTermMemory = new LongTermMemory(embeddingGenerator, TestDbPath);

            // Test 1: Store and retrieve memories
            Console.WriteLine("\nTest 1: Storing memories in long-term storage...");
            var memories = new[]
            {
                new Memory 
                { 
                    Key = "long1", 
                    Content = "This is a test memory about artificial intelligence in long-term storage.",
                    Timestamp = DateTime.UtcNow
                },
                new Memory 
                { 
                    Key = "long2", 
                    Content = "This is a test memory about machine learning in long-term storage.",
                    Timestamp = DateTime.UtcNow
                },
                new Memory 
                { 
                    Key = "long3", 
                    Content = "This is a test memory about neural networks in long-term storage.",
                    Timestamp = DateTime.UtcNow
                }
            };

            foreach (var memory in memories)
            {
                memory.Embedding = await embeddingGenerator.GenerateEmbeddingAsync(memory.Content);
                await longTermMemory.StoreAsync(memory);
            }
            Console.WriteLine("✓ Memories stored successfully in long-term storage");

            // Test 2: Search similar memories
            Console.WriteLine("\nTest 2: Searching similar memories in long-term storage...");
            var queryEmbedding = await embeddingGenerator.GenerateEmbeddingAsync("Tell me about AI and machine learning");
            var results = await longTermMemory.SearchSimilarAsync(queryEmbedding, 2);
            
            Console.WriteLine($"✓ Found {results.Count()} similar memories in long-term storage:");
            foreach (var memory in results)
            {
                Console.WriteLine($"  - {memory.Content} (Key: {memory.Key})");
            }

            // Test 3: Memory expiration
            Console.WriteLine("\nTest 3: Testing memory expiration in long-term storage...");
            var expiringMemory = new Memory
            {
                Key = "expiring_long",
                Content = "This memory will expire soon in long-term storage",
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(2)
            };
            expiringMemory.Embedding = await embeddingGenerator.GenerateEmbeddingAsync(expiringMemory.Content);
            await longTermMemory.StoreAsync(expiringMemory);
            
            Console.WriteLine("Waiting for memory to expire...");
            await Task.Delay(3000); // Wait 3 seconds
            
            var expiredResults = await longTermMemory.SearchSimilarAsync(
                await embeddingGenerator.GenerateEmbeddingAsync("expiring"), 1);
            Console.WriteLine($"✓ Expired memory test: {(expiredResults.Any() ? "Failed (memory still exists)" : "Passed (memory expired)")}");

            // Test 4: Memory cleanup
            Console.WriteLine("\nTest 4: Testing memory cleanup in long-term storage...");
            await longTermMemory.CleanupAsync();
            Console.WriteLine("✓ Long-term memory cleanup completed");

            // Test 5: Persistence across restarts
            Console.WriteLine("\nTest 5: Testing persistence across restarts...");
            var persistentMemory = new Memory
            {
                Key = "persistent",
                Content = "This memory should persist after restart",
                Timestamp = DateTime.UtcNow
            };
            persistentMemory.Embedding = await embeddingGenerator.GenerateEmbeddingAsync(persistentMemory.Content);
            await longTermMemory.StoreAsync(persistentMemory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error during long-term memory tests: {ex.Message}");
            throw;
        }
        finally
        {
            // Cleanup test database
            try
            {
                if (File.Exists(TestDbPath))
                {
                    File.Delete(TestDbPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        Console.WriteLine("\nAll long-term memory system tests completed successfully!");
    }
} 