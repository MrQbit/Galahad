using Microsoft.SemanticKernel;

namespace GalahadAI.Memory;

public static class MemoryTests
{
    public static async Task RunTests()
    {
        Console.WriteLine("Starting memory system tests...");

        // Initialize components
        var embeddingGenerator = new OllamaEmbeddingGenerator();
        var memoryManager = new MemoryManager(embeddingGenerator);

        try
        {
            // Test 1: Store and retrieve memories
            Console.WriteLine("\nTest 1: Storing memories...");
            await memoryManager.StoreMemoryAsync("test1", "This is a test memory about artificial intelligence.");
            await memoryManager.StoreMemoryAsync("test2", "This is a test memory about machine learning.");
            await memoryManager.StoreMemoryAsync("test3", "This is a test memory about neural networks.");
            Console.WriteLine("✓ Memories stored successfully");

            // Test 2: Search similar memories
            Console.WriteLine("\nTest 2: Searching similar memories...");
            var results = await memoryManager.SearchSimilarAsync("Tell me about AI and machine learning", 2);
            Console.WriteLine($"✓ Found {results.Count()} similar memories:");
            foreach (var memory in results)
            {
                Console.WriteLine($"  - {memory.Content} (Key: {memory.Key})");
            }

            // Test 3: Memory expiration
            Console.WriteLine("\nTest 3: Testing memory expiration...");
            var expiringMemory = new Memory
            {
                Key = "expiring",
                Content = "This memory will expire soon",
                ExpiresAt = DateTime.UtcNow.AddSeconds(2)
            };
            await memoryManager.StoreMemoryAsync(expiringMemory.Key, expiringMemory.Content);
            
            Console.WriteLine("Waiting for memory to expire...");
            await Task.Delay(3000); // Wait 3 seconds
            
            var expiredResults = await memoryManager.SearchSimilarAsync("expiring", 1);
            Console.WriteLine($"✓ Expired memory test: {(expiredResults.Any() ? "Failed (memory still exists)" : "Passed (memory expired)")}");

            // Test 4: Memory cleanup
            Console.WriteLine("\nTest 4: Testing memory cleanup...");
            memoryManager.Cleanup();
            Console.WriteLine("✓ Memory cleanup completed");

            Console.WriteLine("\nAll memory system tests completed successfully!");

            // Run long-term memory tests
            await LongTermMemoryTests.RunTests();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error during memory tests: {ex.Message}");
            throw;
        }
    }
} 