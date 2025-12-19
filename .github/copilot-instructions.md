# MudBlazor.Mcp - AI Coding Agent Instructions

## Project Overview

MudBlazor.Mcp is an MCP (Model Context Protocol) server that provides AI assistants with access to MudBlazor component documentation. It clones the MudBlazor repository, parses source files using Roslyn, and exposes an indexed API via MCP tools.

**Tech Stack:** .NET 10, ASP.NET Core, Roslyn, LibGit2Sharp, xUnit + Moq

## Architecture

```
MCP Tools (12 tools) → ComponentIndexer → Parsing Services → GitRepositoryService
                              ↓
                      In-memory index of ~85 components
```

**Key services:**
- [ComponentIndexer.cs](../src/MudBlazor.Mcp/Services/ComponentIndexer.cs) - Builds/queries the component index
- [XmlDocParser.cs](../src/MudBlazor.Mcp/Services/Parsing/XmlDocParser.cs) - Parses C# source using Roslyn
- [GitRepositoryService.cs](../src/MudBlazor.Mcp/Services/GitRepositoryService.cs) - Clones/updates MudBlazor repo
- [Tools/](../src/MudBlazor.Mcp/Tools/) - MCP tool implementations with `[McpServerTool]` attributes

## Build & Test Commands

```bash
# Build (from repo root)
dotnet build

# Run tests
dotnet test --no-build

# Run server (HTTP transport on localhost:5180)
cd src/MudBlazor.Mcp && dotnet run

# Run server (stdio transport for CLI clients)
cd src/MudBlazor.Mcp && dotnet run -- --stdio
```

## Code Patterns

### MCP Tools Pattern
Tools are static methods with DI parameters and `[McpServerTool]` + `[Description]` attributes:

```csharp
[McpServerToolType]
public sealed class ComponentDetailTools
{
    [McpServerTool(Name = "get_component_detail")]
    [Description("Gets comprehensive details about a MudBlazor component.")]
    public static async Task<string> GetComponentDetailAsync(
        IComponentIndexer indexer,           // DI injected
        ILogger<ComponentDetailTools> logger,
        [Description("Component name")] string componentName,  // Tool parameter
        CancellationToken cancellationToken = default)
    {
        ToolValidation.RequireNonEmpty(componentName, nameof(componentName));
        // ... implementation
    }
}
```

### Error Handling in Tools
Use `ToolValidation` for consistent MCP-friendly errors that LLMs can self-correct:

```csharp
ToolValidation.RequireNonEmpty(componentName, nameof(componentName));
ToolValidation.ThrowComponentNotFound(componentName);  // Suggests list_components
```

### Domain Models
All models in [Models/ComponentInfo.cs](../src/MudBlazor.Mcp/Models/ComponentInfo.cs) are immutable records:
- `ComponentInfo` - Component with parameters, events, methods, examples
- `ComponentParameter`, `ComponentEvent`, `ComponentMethod`, `ComponentExample`
- `ApiReference`, `ComponentCategory`

## Testing Conventions

- Tests in `tests/MudBlazor.Mcp.Tests/` mirror `src/` structure
- Use Moq for interface mocking, xUnit for assertions
- Use `NullLoggerFactory.Instance.CreateLogger<T>()` for test loggers
- Tool tests should verify both success and `McpException` error cases

Example pattern from [ComponentDetailToolsTests.cs](../tests/MudBlazor.Mcp.Tests/Tools/ComponentDetailToolsTests.cs):
```csharp
[Fact]
public async Task GetComponentDetailAsync_WithInvalidComponent_ThrowsMcpException()
{
    var indexer = new Mock<IComponentIndexer>();
    indexer.Setup(x => x.GetComponentAsync("Unknown", It.IsAny<CancellationToken>()))
        .ReturnsAsync((ComponentInfo?)null);

    var ex = await Assert.ThrowsAsync<McpException>(...);
    Assert.Contains("not found", ex.Message);
}
```

## Key Files to Understand

| Purpose | Location |
|---------|----------|
| Startup/DI | [Program.cs](../src/MudBlazor.Mcp/Program.cs) |
| Service interfaces | [Services/IComponentIndexer.cs](../src/MudBlazor.Mcp/Services/IComponentIndexer.cs) |
| Roslyn parsing | [Services/Parsing/XmlDocParser.cs](../src/MudBlazor.Mcp/Services/Parsing/XmlDocParser.cs) |
| Configuration | [Configuration/MudBlazorOptions.cs](../src/MudBlazor.Mcp/Configuration/MudBlazorOptions.cs) |
| Tool validation | [Tools/ToolValidation.cs](../src/MudBlazor.Mcp/Tools/ToolValidation.cs) |

## Project-Specific Notes

- The `data/mudblazor-repo/` folder is cloned at runtime and excluded from compilation via `<DefaultItemExcludes>`
- Server supports both HTTP (`/mcp` endpoint) and stdio transports via `--stdio` flag
- Health checks at `/health`, `/health/ready`, `/health/live`
- All logging goes to stderr for MCP protocol compatibility
- Component names support flexible lookup: "Button" resolves to "MudButton"

## License

GPL-2.0 - Include copyright header in all source files:
```csharp
// Copyright (c) 2025 MudBlazor.Mcp Contributors
// Licensed under the GNU General Public License v2.0.
```
