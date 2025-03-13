# Galahad AI - Project Index

## Quick Links
- [README.md](README.md) - Project overview and setup instructions
- [ARCHITECTURE.md](ARCHITECTURE.md) - Detailed system architecture
- [DEVELOPMENT_NOTES.md](DEVELOPMENT_NOTES.md) - Current development status and decisions
- [VERSION_HISTORY.md](VERSION_HISTORY.md) - Version tracking and changes
- [PLAN.MD](PLAN.MD) - Implementation plan and roadmap

## Project Structure

### Documentation
```
docs/
├── Core/
│   ├── Memory System.md
│   ├── Evolution System.md
│   └── Agent System.md
├── Architecture/
│   ├── System Design.md
│   ├── Security Model.md
│   └── Deployment Guide.md
└── Development/
    ├── Getting Started.md
    ├── Contributing.md
    └── Testing Guide.md
```

### Source Code
```
GalahadAI/
├── Agents/
│   ├── Base/
│   │   ├── IAgent.cs
│   │   └── BaseAgent.cs
│   └── Specialized/
│       ├── TaskAgent.cs
│       ├── KnowledgeAgent.cs
│       └── UIAgent.cs
├── Communication/
│   ├── MessageBus.cs
│   └── Protocols/
├── Memory/
│   ├── ShortTerm/
│   ├── WarmMemory/
│   └── LongTerm/
├── Evolution/
│   ├── DistributedEvolution/
│   └── PatternRecognition/
├── Security/
│   ├── Authentication/
│   └── Authorization/
└── Tests/
    ├── UnitTests/
    ├── IntegrationTests/
    └── PerformanceTests/
```

## Core Components

### 1. Memory System
- [Short-term Memory](GalahadAI/Memory/ShortTerm/) - Volatile memory management
- [Warm Memory](GalahadAI/Memory/WarmMemory/) - File-based persistent storage
- [Long-term Memory](GalahadAI/Memory/LongTerm/) - SQLite-based storage
- [Memory Sharing](GalahadAI/Memory/Shared/) - Multi-agent memory management

### 2. Agent System
- [Base Agent Framework](GalahadAI/Agents/Base/)
- [Specialized Agents](GalahadAI/Agents/Specialized/)
- [Agent Communication](GalahadAI/Communication/)
- [Agent Coordination](GalahadAI/Agents/Coordination/)

### 3. Evolution System
- [Distributed Evolution](GalahadAI/Evolution/DistributedEvolution/)
- [Pattern Recognition](GalahadAI/Evolution/PatternRecognition/)
- [Learning Repository](GalahadAI/Evolution/Learning/)

## Key Interfaces

### Agent Interfaces
```csharp
// GalahadAI/Agents/Base/IAgent.cs
public interface IAgent
{
    string AgentId { get; }
    AgentType Type { get; }
    AgentState State { get; }
    IEnumerable<ICapability> Capabilities { get; }
    
    Task<bool> InitializeAsync();
    Task<bool> ShutdownAsync();
    Task<AgentResponse> ProcessMessageAsync(AgentMessage message);
    Task<bool> RegisterCapabilityAsync(ICapability capability);
}
```

### Memory Interfaces
```csharp
// GalahadAI/Memory/ISharedMemory.cs
public interface ISharedMemory
{
    Task<bool> AcquireLockAsync(string resourceId, TimeSpan timeout);
    Task<T> ReadSharedMemoryAsync<T>(string key);
    Task WriteSharedMemoryAsync<T>(string key, T value);
    Task ReleaseLockAsync(string resourceId);
    Task<IEnumerable<string>> ListKeysAsync(string pattern);
}
```

### Evolution Interfaces
```csharp
// GalahadAI/Evolution/IEvolutionManager.cs
public interface IEvolutionManager
{
    Task<bool> ProposeEvolutionAsync(EvolutionProposal proposal);
    Task<EvolutionResult> EvolveAsync(CancellationToken cancellationToken);
    Task<bool> ValidateEvolutionAsync(EvolutionProposal proposal);
}
```

## Development Workflow

### 1. Setup
1. Clone repository
2. Install dependencies
3. Configure local services
4. Build solution

### 2. Development
1. Create feature branch
2. Implement changes
3. Add tests
4. Update documentation

### 3. Testing
1. Run unit tests
2. Run integration tests
3. Perform security checks
4. Validate performance

### 4. Deployment
1. Build release
2. Run deployment tests
3. Update documentation
4. Create release notes

## Current Status

### Active Development
- Multi-agent architecture implementation
- Agent communication protocol
- Memory sharing system
- Evolution synchronization

### Completed Features
- Memory system migration ✓
- Evolution system adaptation ✓
- Documentation framework ✓
- Project restructuring ✓

### Next Steps
1. Implement base agent system
2. Create communication infrastructure
3. Set up memory sharing
4. Add agent specialization

## Resources

### Documentation
- [Architecture Overview](ARCHITECTURE.md)
- [Development Guide](DEVELOPMENT_NOTES.md)
- [API Documentation](docs/API/)
- [Testing Guide](docs/Testing/)

### External Links
- [.NET 9.0 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [gRPC Documentation](https://grpc.io/docs/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [Redis Documentation](https://redis.io/documentation)

### Tools
- LM Studio
- Ollama
- SQLite
- Redis/RabbitMQ

## Support

### Getting Help
- [Issue Tracker](issues/)
- [Wiki](wiki/)
- [FAQ](docs/FAQ.md)

### Contributing
- [Contributing Guide](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Style Guide](docs/STYLE_GUIDE.md)

## License
- [License Information](LICENSE)
- [Third-party Licenses](THIRD_PARTY_LICENSES.md) 