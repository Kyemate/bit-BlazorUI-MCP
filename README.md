# MudBlazor MCP Server

An MCP (Model Context Protocol) server that provides AI assistants with access to MudBlazor component documentation, examples, and usage information.

> **Note:** This project is not affiliated with MudBlazor. It extracts documentation from the official MudBlazor repository.

## Features

- **Component Discovery**: List all MudBlazor components with filtering by category
- **Detailed Documentation**: Get comprehensive component details including parameters, events, and methods
- **Code Examples**: Access real code examples from the MudBlazor documentation
- **Search**: Search components by name, description, or parameters
- **API Reference**: Get full API reference for components and enums
- **Related Components**: Discover related components through inheritance and common usage

## Available MCP Tools

| Tool | Description |
|------|-------------|
| `list_components` | Lists all MudBlazor components, optionally filtered by category |
| `list_categories` | Lists all component categories with descriptions |
| `get_component_detail` | Gets comprehensive details about a specific component |
| `get_component_parameters` | Gets all parameters for a component |
| `get_component_examples` | Gets code examples for a component |
| `get_example_by_name` | Gets a specific example by name |
| `list_component_examples` | Lists all example names for a component |
| `search_components` | Searches components by query |
| `get_components_by_category` | Gets all components in a specific category |
| `get_related_components` | Gets components related to a specific component |
| `get_api_reference` | Gets full API reference for a type |
| `get_enum_values` | Gets all values for a MudBlazor enum |

## Prerequisites

- .NET 10 SDK (Preview)
- Git

## Getting Started

### Clone and Build

```bash
git clone https://github.com/yourusername/MudBlazor.Mcp.git
cd MudBlazor.Mcp
dotnet restore
dotnet build
```

### Run with HTTP Transport (Default)

```bash
cd src/MudBlazor.Mcp
dotnet run
```

The server will:
1. Clone the MudBlazor repository (or update if it exists)
2. Index all component documentation
3. Start the MCP server on `http://localhost:5180`

### Run with stdio Transport (for CLI clients)

```bash
cd src/MudBlazor.Mcp
dotnet run -- --stdio
```

Use stdio transport when integrating with CLI-based MCP clients that communicate via standard input/output.

### Run with Aspire

```bash
cd src/MudBlazor.Mcp.AppHost
dotnet run
```

## Testing the Server

### Testing with HTTP Transport

#### 1. Verify the Server is Running

```bash
# Check health endpoint
curl http://localhost:5180/health
```

Expected response:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "indexer",
      "status": "Healthy",
      "description": "Index contains 85 components in 12 categories.",
      "data": {
        "status": "ready",
        "componentCount": 85,
        "categoryCount": 12,
        "isIndexed": true
      }
    }
  ]
}
```

#### 2. Test with MCP Inspector

```bash
npx @modelcontextprotocol/inspector
```

#### 3. Test Tool Calls Directly

Using `curl` to test the MCP endpoint:

```bash
# List all tools
curl -X POST http://localhost:5180/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "tools/list", "id": 1}'

# Call list_components tool
curl -X POST http://localhost:5180/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "list_components",
      "arguments": {"category": "Buttons"}
    },
    "id": 2
  }'

# Call get_component_detail tool
curl -X POST http://localhost:5180/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "get_component_detail",
      "arguments": {"componentName": "MudButton", "includeExamples": true}
    },
    "id": 3
  }'

# Search for components
curl -X POST http://localhost:5180/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "search_components",
      "arguments": {"query": "date picker", "maxResults": 5}
    },
    "id": 4
  }'
```

### Testing with stdio Transport

```bash
# Start the server in stdio mode
dotnet run -- --stdio

# The server reads JSON-RPC messages from stdin and writes responses to stdout
# You can pipe commands or use a compatible MCP client
```

Example JSON-RPC message to send via stdin:
```json
{"jsonrpc": "2.0", "method": "tools/list", "id": 1}
```

## Configuration

Configure the server via `appsettings.json` or environment variables:

```json
{
  "MudBlazor": {
    "Repository": {
      "Url": "https://github.com/MudBlazor/MudBlazor.git",
      "Branch": "dev",
      "LocalPath": "./mudblazor-repo",
      "AutoUpdate": true
    },
    "Cache": {
      "Enabled": true,
      "SlidingExpirationMinutes": 60,
      "AbsoluteExpirationMinutes": 1440
    }
  }
}
```

## Using with AI Assistants

### VS Code with GitHub Copilot

Add to your VS Code settings (`.vscode/mcp.json`):

```json
{
  "servers": {
    "mudblazor": {
      "url": "http://localhost:5180/mcp"
    }
  }
}
```

Or for stdio transport, add to VS Code settings:

```json
{
  "servers": {
    "mudblazor": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/MudBlazor.Mcp", "--", "--stdio"]
    }
  }
}
```

### Claude Desktop

Add to your Claude configuration (`claude_desktop_config.json`):

For HTTP transport:
```json
{
  "mcpServers": {
    "mudblazor": {
      "url": "http://localhost:5180/mcp"
    }
  }
}
```

For stdio transport:
```json
{
  "mcpServers": {
    "mudblazor": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/MudBlazor.Mcp", "--", "--stdio"]
    }
  }
}
```

## Example Usage

Once connected, you can ask your AI assistant questions like:

- "List all MudBlazor button components"
- "Show me how to use MudTextField with validation"
- "What parameters does MudDataGrid support?"
- "Show me examples of MudDialog"
- "What are the available Color enum values?"
- "Find components related to MudSelect"

## Troubleshooting

### Server doesn't start
- Ensure .NET 10 SDK is installed: `dotnet --version`
- Check that port 5180 is available
- Review logs in the terminal for error messages

### No components found
- The index builds on startup; wait for "Index built successfully" log message
- Check `/health` endpoint for index status
- Verify the MudBlazor repository was cloned successfully in `./data/mudblazor-repo`

### Git clone fails
- Ensure network access to GitHub
- Check sufficient disk space (MudBlazor repo is ~500MB)
- Verify git is installed: `git --version`

### Tools not discovered
- Verify `[McpServerToolType]` and `[McpServerTool]` attributes are present
- Check that the assembly is being scanned with `WithToolsFromAssembly()`
- Review server logs for any startup errors

### stdio transport issues
- Ensure logging goes to stderr (configured by default)
- Don't write anything to stdout except MCP responses
- Use `--stdio` flag when starting the server

## Project Structure

```
src/
├── MudBlazor.Mcp/              # Main MCP server
│   ├── Configuration/          # Options and settings
│   ├── Models/                 # Domain models
│   ├── Services/               # Core services
│   │   └── Parsing/            # Parsing utilities
│   └── Tools/                  # MCP tools
├── MudBlazor.Mcp.AppHost/      # Aspire orchestration
└── MudBlazor.Mcp.ServiceDefaults/  # Shared service defaults

tests/
└── MudBlazor.Mcp.Tests/        # Unit tests
```

## Architecture

The server follows a clean architecture pattern:

1. **Git Repository Service**: Clones and keeps the MudBlazor repository up to date
2. **Parsing Services**: Extract documentation from source files using Roslyn
3. **Component Indexer**: Builds and maintains the searchable component index
4. **MCP Tools**: Expose the indexed data through MCP protocol

```
┌─────────────────────────────────────────────────────────────┐
│                      MCP Tools                              │
│  (list_components, get_component_detail, search, etc.)      │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Component Indexer                         │
│           (In-memory index of all components)               │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   Parsing Services                          │
│  XmlDocParser │ RazorDocParser │ ExampleExtractor           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                 Git Repository Service                      │
│              (Clone/Update MudBlazor repo)                  │
└─────────────────────────────────────────────────────────────┘
```

## Health Checks

- `/health` - Overall health status with detailed JSON response
- `/health/ready` - Readiness check (index built)
- `/health/live` - Liveness check

## Security & Production Considerations

### Rate Limiting / Resource Protection

This server does not include built-in rate limiting or request throttling. When deploying to production, especially in shared or public-facing environments, consider implementing the following protections:

1. **API Gateway / Reverse Proxy**: Deploy behind a reverse proxy (NGINX, YARP, Azure API Management) that provides:
   - Request rate limiting
   - IP-based throttling
   - Request size limits
   - DDoS protection

2. **ASP.NET Core Rate Limiting**: Add the built-in rate limiting middleware:
   ```csharp
   builder.Services.AddRateLimiter(options =>
   {
       options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
           RateLimitPartition.GetFixedWindowLimiter(
               partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
               factory: _ => new FixedWindowRateLimiterOptions
               {
                   AutoReplenishment = true,
                   PermitLimit = 100,
                   Window = TimeSpan.FromMinutes(1)
               }));
   });
   ```

3. **Timeout Policies**: Configure request timeouts to prevent long-running operations:
   ```csharp
   builder.WebHost.ConfigureKestrel(options =>
   {
       options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
       options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
   });
   ```

4. **Request Size Limits**: Limit the size of incoming requests:
   ```csharp
   builder.Services.Configure<KestrelServerOptions>(options =>
   {
       options.Limits.MaxRequestBodySize = 1_048_576; // 1MB
   });
   ```

5. **Authentication & Authorization**: For sensitive deployments, implement authentication:
   - API key authentication
   - JWT bearer tokens
   - Client certificates

6. **Resource Constraints**: Monitor and limit resource consumption:
   - Memory limits via container orchestration
   - CPU throttling
   - Connection pool limits

### Additional Security Recommendations

- **HTTPS Only**: Always deploy with TLS enabled in production
- **CORS Configuration**: Restrict allowed origins if accessed from browsers
- **Audit Logging**: Log all API access for security monitoring
- **Health Check Security**: Restrict health endpoints to internal networks if they expose sensitive information

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project, MudBlazor.Mcp, is an independent implementation built on top of the MudBlazor framework. It is not affiliated with, endorsed by, or officially supported by the MudBlazor team.
MudBlazor is licensed under the GNU General Public License v2.0 (GPL-2.0). In compliance with this license:

The source code of this project is provided under GPL-2.0.
Original copyright and license notices from MudBlazor are retained.
Modifications and additions are clearly documented.

For more details on the GPL-2.0 license, see GNU GPL v2.0 [LICENSE](LICENSE) file for details.

## Acknowledgments

- [MudBlazor](https://mudblazor.com/) - The amazing Blazor component library
- [Model Context Protocol](https://modelcontextprotocol.io/) - The protocol specification
