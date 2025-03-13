# Contributing to Galahad AI

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/yourusername/galahad.git`
3. Create a new branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Push to your fork: `git push origin feature/your-feature-name`
6. Open a Pull Request

## Development Environment

### Prerequisites
- .NET 9.0 SDK
- LM Studio
- Ollama
- SQLite
- Redis or RabbitMQ (for message bus)

### Setup
1. Install dependencies
2. Configure local services
3. Build solution: `dotnet build`
4. Run tests: `dotnet test`

## Code Style

### General Guidelines
- Follow C# coding conventions
- Use async/await for asynchronous operations
- Implement interface-based design
- Follow SOLID principles
- Write self-documenting code

### Naming Conventions
- PascalCase for types and methods
- camelCase for variables and parameters
- Use descriptive names
- Avoid abbreviations

### Documentation
- XML comments for public APIs
- README updates for new features
- Keep architecture documentation current
- Update version history

## Testing

### Requirements
- Unit tests for all new code
- Integration tests for components
- Performance tests for critical paths
- Security tests for sensitive areas

### Test Guidelines
- Use meaningful test names
- Follow Arrange-Act-Assert pattern
- Mock external dependencies
- Test edge cases
- Include performance benchmarks

## Pull Request Process

1. Update documentation
2. Add/update tests
3. Ensure CI passes
4. Request review
5. Address feedback
6. Update changelog

## Commit Messages

### Format
```
type(scope): subject

body

footer
```

### Types
- feat: New feature
- fix: Bug fix
- docs: Documentation
- style: Formatting
- refactor: Code restructuring
- test: Adding tests
- chore: Maintenance

### Example
```
feat(agents): add task distribution system

Implement task distribution among specialized agents
with priority queue and load balancing.

Closes #123
```

## Release Process

1. Version bump
2. Update changelog
3. Create release notes
4. Tag release
5. Deploy to staging
6. Verify deployment
7. Deploy to production

## Community

### Communication
- Use issue tracker for bugs
- Discussions for features
- Pull requests for changes
- Wiki for documentation

### Support
- Check existing issues
- Search documentation
- Ask in discussions
- Follow code of conduct

## License

By contributing, you agree that your contributions will be licensed under the project's license. 