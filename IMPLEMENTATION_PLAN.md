# MudBlazor MCP Server - Implementation Plan

## Executive Summary

This document outlines the implementation plan for an MCP (Model Context Protocol) server that provides AI assistants with comprehensive access to MudBlazor component documentation, examples, and usage information.

## 1. MudBlazor Repository Analysis

### Key Directories Structure

```
MudBlazor/
├── src/
│   ├── MudBlazor/                          # Core component library
│   │   ├── Components/                     # Component implementations
│   │   │   ├── Button/
│   │   │   │   ├── MudButton.razor         # Component markup
│   │   │   │   └── MudButton.razor.cs      # Component code-behind with XML docs
│   │   │   ├── Card/
│   │   │   ├── Table/
│   │   │   └── ... (~80+ component folders)
│   │   ├── Enums/                          # Enumerations
│   │   ├── Attributes/                     # CategoryAttribute defines categories
│   │   └── Styles/                         # SCSS styles
│   │
│   ├── MudBlazor.Docs/                     # Documentation website source
│   │   ├── Pages/
│   │   │   ├── Components/                 # Component documentation pages
│   │   │   │   ├── Button/
│   │   │   │   │   ├── ButtonPage.razor    # Main docs page
│   │   │   │   │   └── Examples/           # Example components
│   │   │   │   │       ├── ButtonSimpleExample.razor
│   │   │   │   │       └── ...
│   │   │   ├── Api/                        # API reference pages
│   │   │   └── Features/                   # Feature documentation
│   │   ├── Models/
│   │   │   ├── Generated/
│   │   │   │   ├── ApiDocumentation.cs     # Auto-generated API docs
│   │   │   │   └── DocumentedType.cs       # Type documentation models
│   │   │   └── Snippets.cs                 # Code snippets helper
│   │   ├── Services/
│   │   │   ├── Menu/
│   │   │   │   ├── IMenuService.cs         # Component categorization
│   │   │   │   └── MenuService.cs          # Category definitions
│   │   │   └── Navigation/                 # Navigation services
│   │   └── Components/                     # Docs UI components
│   │
│   └── MudBlazor.Docs.Compiler/           # Documentation generator
│       ├── ApiDocumentationBuilder.cs      # XML doc parser
│       ├── ApiDocumentationWriter.cs       # Documentation serializer
│       ├── CodeSnippets.cs                 # Example code extractor
│       └── ExamplesMarkup.cs               # Markup processor
```

### Key Findings for Parsing

1. **Component Documentation Sources:**
   - **XML Comments**: Located in `.razor.cs` files with comprehensive `<summary>`, `<remarks>`, `<param>` tags
   - **Category Information**: `[Category(CategoryTypes.Button.Behavior)]` attributes organize parameters
   - **Documentation Pages**: `.razor` files in `Pages/Components/` contain usage guidance
   - **Examples**: Named with `*Example.razor` pattern in `Examples/` subfolders

2. **Component Categories (from MenuService.cs):**
   - Form & Inputs (Radio, CheckBox, Select, TextField, etc.)
   - Buttons (Button, ButtonGroup, IconButton, FAB)
   - Charts (Donut, Line, Pie, Bar, etc.)
   - Pickers (DatePicker, TimePicker, ColorPicker)
   - Layout (Container, Grid, Stack, etc.)
   - Navigation (NavMenu, Tabs, Breadcrumbs)
   - Data Display (Table, DataGrid, List)
   - Feedback (Alert, Dialog, Snackbar)
   - And more...

3. **API Documentation Model:**
   - `DocumentedType`: Component type information
   - `DocumentedProperty`: Parameter documentation
   - `DocumentedMethod`: Public method documentation
   - `DocumentedEvent`: Event callback documentation

---

## 2. Solution Architecture

### Project Structure

```
MudBlazor.Mcp/
├── src/
│   ├── MudBlazor.Mcp/                      # Main MCP server
│   │   ├── Program.cs                      # Entry point
│   │   ├── MudBlazor.Mcp.csproj
│   │   ├── appsettings.json
│   │   │
│   │   ├── Configuration/
│   │   │   └── MudBlazorOptions.cs         # Configuration model
│   │   │
│   │   ├── Services/
│   │   │   ├── IGitRepositoryService.cs
│   │   │   ├── GitRepositoryService.cs     # Clone/update MudBlazor repo
│   │   │   ├── IComponentIndexer.cs
│   │   │   ├── ComponentIndexer.cs         # Parse and index components
│   │   │   ├── IDocumentationCache.cs
│   │   │   └── DocumentationCache.cs       # In-memory caching
│   │   │
│   │   ├── Models/
│   │   │   ├── ComponentInfo.cs            # Component metadata
│   │   │   ├── ComponentParameter.cs       # Parameter details
│   │   │   ├── ComponentExample.cs         # Code example
│   │   │   ├── ComponentCategory.cs        # Category grouping
│   │   │   └── ApiReference.cs             # API documentation
│   │   │
│   │   ├── Parsing/
│   │   │   ├── RazorFileParser.cs          # Parse .razor files
│   │   │   ├── XmlDocParser.cs             # Parse XML comments
│   │   │   ├── ExampleExtractor.cs         # Extract examples
│   │   │   └── CategoryMapper.cs           # Map to categories
│   │   │
│   │   └── Tools/
│   │       ├── ComponentListTools.cs       # list_components tool
│   │       ├── ComponentDetailTools.cs     # get_component_detail tool
│   │       ├── ComponentSearchTools.cs     # search_components tools
│   │       ├── ComponentExampleTools.cs    # get_component_examples tool
│   │       └── ApiReferenceTools.cs        # get_api_reference tool
│   │
│   ├── MudBlazor.Mcp.AppHost/             # .NET Aspire orchestration
│   │   ├── Program.cs
│   │   └── MudBlazor.Mcp.AppHost.csproj
│   │
│   └── MudBlazor.Mcp.ServiceDefaults/     # Shared configurations
│       ├── Extensions.cs
│       └── MudBlazor.Mcp.ServiceDefaults.csproj
│
├── tests/
│   └── MudBlazor.Mcp.Tests/
│       ├── Tools/
│       ├── Services/
│       └── Parsing/
│
├── MudBlazor.Mcp.sln
└── README.md
```

---

## 3. MCP Tools Specification

### Naming Convention
Following MCP best practices, tool names use `snake_case` and follow a clear verb-noun pattern.

### Tool Definitions

#### 3.1 `list_components`
Lists all available MudBlazor components with basic information.

```json
{
  "name": "list_components",
  "description": "Lists all available MudBlazor components with their names, descriptions, and categories. Use this to discover available components or browse by category.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "category": {
        "type": "string",
        "description": "Optional category filter (e.g., 'Form Inputs', 'Buttons', 'Charts', 'Layout')"
      },
      "includeChildComponents": {
        "type": "boolean",
        "description": "Include related child components in the listing (default: false)",
        "default": false
      }
    }
  }
}
```

**Output Format:**
```json
{
  "components": [
    {
      "name": "MudButton",
      "displayName": "Button",
      "category": "Buttons",
      "description": "A Material Design button with various styles and states",
      "hasExamples": true,
      "childComponents": ["MudButtonGroup"]
    }
  ],
  "totalCount": 85,
  "categories": ["Buttons", "Form Inputs", "Charts", ...]
}
```

#### 3.2 `get_component_detail`
Returns comprehensive documentation for a specific component.

```json
{
  "name": "get_component_detail",
  "description": "Gets detailed documentation for a specific MudBlazor component including description, parameters, events, methods, and usage notes.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "componentName": {
        "type": "string",
        "description": "The component name (e.g., 'MudButton', 'MudTextField', 'MudDataGrid')"
      },
      "includeExamples": {
        "type": "boolean",
        "description": "Include code examples in the response (default: true)",
        "default": true
      },
      "includeInheritedMembers": {
        "type": "boolean",
        "description": "Include inherited parameters and methods (default: false)",
        "default": false
      }
    },
    "required": ["componentName"]
  }
}
```

**Output Format:**
```json
{
  "name": "MudButton",
  "namespace": "MudBlazor",
  "description": "A Material Design button...",
  "remarks": "Buttons allow users to take actions...",
  "baseType": "MudBaseButton",
  "category": "Buttons",
  "parameters": [
    {
      "name": "Color",
      "type": "Color",
      "defaultValue": "Color.Default",
      "category": "Appearance",
      "description": "The color of the button"
    }
  ],
  "events": [
    {
      "name": "OnClick",
      "type": "EventCallback<MouseEventArgs>",
      "description": "Callback when the button is clicked"
    }
  ],
  "methods": [...],
  "examples": [
    {
      "name": "Basic Usage",
      "code": "<MudButton Variant=\"Variant.Filled\">Click Me</MudButton>",
      "description": "A simple filled button"
    }
  ],
  "relatedComponents": ["MudIconButton", "MudFab", "MudButtonGroup"],
  "docsUrl": "https://mudblazor.com/components/button"
}
```

#### 3.3 `search_components`
Searches components by name, description, or functionality.

```json
{
  "name": "search_components",
  "description": "Searches MudBlazor components by name, description, or functionality. Useful for finding components that match specific requirements.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "query": {
        "type": "string",
        "description": "Search query (searches names, descriptions, and parameter names)"
      },
      "searchIn": {
        "type": "array",
        "items": {
          "type": "string",
          "enum": ["name", "description", "parameters", "examples"]
        },
        "description": "Where to search (default: all fields)",
        "default": ["name", "description", "parameters"]
      },
      "maxResults": {
        "type": "integer",
        "description": "Maximum number of results (default: 10)",
        "default": 10
      }
    },
    "required": ["query"]
  }
}
```

#### 3.4 `get_components_by_category`
Returns all components in a specific category.

```json
{
  "name": "get_components_by_category",
  "description": "Gets all MudBlazor components in a specific category (e.g., 'Form Inputs', 'Buttons', 'Charts', 'Layout', 'Navigation').",
  "inputSchema": {
    "type": "object",
    "properties": {
      "category": {
        "type": "string",
        "description": "The category name (e.g., 'Form Inputs', 'Buttons', 'Charts')"
      },
      "includeDescriptions": {
        "type": "boolean",
        "description": "Include component descriptions (default: true)",
        "default": true
      }
    },
    "required": ["category"]
  }
}
```

#### 3.5 `get_component_examples`
Returns all code examples for a component.

```json
{
  "name": "get_component_examples",
  "description": "Gets all code examples and usage patterns for a specific MudBlazor component.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "componentName": {
        "type": "string",
        "description": "The component name (e.g., 'MudButton')"
      },
      "exampleType": {
        "type": "string",
        "enum": ["basic", "advanced", "all"],
        "description": "Type of examples to return (default: 'all')",
        "default": "all"
      }
    },
    "required": ["componentName"]
  }
}
```

#### 3.6 `get_component_parameters`
Returns detailed parameter documentation for a component.

```json
{
  "name": "get_component_parameters",
  "description": "Gets detailed parameter documentation for a MudBlazor component including types, defaults, and categories.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "componentName": {
        "type": "string",
        "description": "The component name"
      },
      "category": {
        "type": "string",
        "description": "Filter by parameter category (e.g., 'Appearance', 'Behavior')"
      },
      "includeInherited": {
        "type": "boolean",
        "description": "Include inherited parameters (default: true)",
        "default": true
      }
    },
    "required": ["componentName"]
  }
}
```

#### 3.7 `get_api_reference`
Returns API reference for types, enums, and services.

```json
{
  "name": "get_api_reference",
  "description": "Gets API reference documentation for MudBlazor types, enumerations, and services.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "typeName": {
        "type": "string",
        "description": "The type name (e.g., 'Color', 'Variant', 'Size', 'ISnackbar')"
      }
    },
    "required": ["typeName"]
  }
}
```

#### 3.8 `get_related_components`
Finds components related to a specific component.

```json
{
  "name": "get_related_components",
  "description": "Finds MudBlazor components related to a specific component (siblings, parent/child relationships, common usage patterns).",
  "inputSchema": {
    "type": "object",
    "properties": {
      "componentName": {
        "type": "string",
        "description": "The component name"
      },
      "relationshipType": {
        "type": "string",
        "enum": ["all", "parent", "child", "sibling", "commonly_used_with"],
        "description": "Type of relationship to find (default: 'all')",
        "default": "all"
      }
    },
    "required": ["componentName"]
  }
}
```

---

## 4. Parsing Strategy

### 4.1 XML Documentation Parsing

MudBlazor uses comprehensive XML documentation. Parse from `.razor.cs` files:

```csharp
/// <summary>
/// A Material Design button with various styles and states.
/// </summary>
/// <seealso cref="MudIconButton"/>
/// <seealso cref="MudFab"/>
public partial class MudButton : MudBaseButton
{
    /// <summary>
    /// The color of the button.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Color.Default"/>.
    /// </remarks>
    [Parameter]
    [Category(CategoryTypes.Button.Appearance)]
    public Color Color { get; set; } = Color.Default;
}
```

**Parsing approach:**
1. Use Roslyn to parse C# files and extract XML comments
2. Build a `DocumentedType` model for each component
3. Resolve `<see cref="">` links to related types
4. Extract `[Category]` attributes for parameter grouping

### 4.2 Documentation Page Parsing

Parse `.razor` documentation pages for descriptions and sections:

```razor
@page "/components/button"

<DocsPage>
    <DocsPageHeader Title="Button" Component="@nameof(MudButton)">
        <Description>
            Buttons allow users to take actions with a single tap.
        </Description>
    </DocsPageHeader>
    
    <DocsPageSection>
        <SectionHeader Title="Basic Usage">
            <Description>Simple button examples...</Description>
        </SectionHeader>
        <SectionContent Code="@nameof(ButtonSimpleExample)">
            <ButtonSimpleExample />
        </SectionContent>
    </DocsPageSection>
</DocsPage>
```

**Parsing approach:**
1. Use regex or a simple Razor parser to extract:
   - Page title and description
   - Section headers and descriptions
   - Example component references
2. Map sections to the component documentation

### 4.3 Example Code Extraction

Extract example code from `*Example.razor` files:

```razor
@namespace MudBlazor.Docs.Examples

<MudButton Variant="Variant.Filled" Color="Color.Primary">
    Primary Button
</MudButton>
<MudButton Variant="Variant.Outlined">
    Outlined Button
</MudButton>
```

**Parsing approach:**
1. Read all files matching `*Example.razor` pattern
2. Extract the markup (everything after `@namespace` and `@code` declarations)
3. Associate with parent component via folder structure or naming convention
4. Clean up and format for output

### 4.4 Category Mapping

Categories are defined in `MenuService.cs`. Parse this to build a category map:

```csharp
.AddNavGroup("Form & Inputs", false, new DocsComponents()
    .AddItem("Radio", typeof(MudRadio<T>))
    .AddItem("Check Box", typeof(MudCheckBox<T>))
    // ...
)
```

**Parsing approach:**
1. Parse `MenuService.cs` to extract navigation groups
2. Build a `Dictionary<string, List<string>>` of category → components
3. Use this for category filtering in tools

---

## 5. Caching Strategy

### 5.1 Multi-Level Cache

```csharp
public interface IDocumentationCache
{
    Task<ComponentInfo?> GetComponentAsync(string name);
    Task SetComponentAsync(string name, ComponentInfo info);
    Task<IReadOnlyList<ComponentInfo>> GetAllComponentsAsync();
    Task InvalidateAsync();
    Task<DateTimeOffset> GetLastRefreshTimeAsync();
}

public class DocumentationCache : IDocumentationCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache; // Optional for scaling
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    
    // Cache components by name with sliding expiration
    // Full index with absolute expiration
    // Example code cached separately with longer TTL
}
```

### 5.2 Refresh Strategy

- **On Startup**: Full index build if cache is empty or stale
- **Background Refresh**: Periodic check for repo updates (configurable interval)
- **On-Demand**: Force refresh via admin endpoint or tool

### 5.3 Cache Keys

```
mudblazor:version                    → Repository commit hash
mudblazor:components:index           → Full component list
mudblazor:component:{name}           → Individual component details
mudblazor:component:{name}:examples  → Component examples
mudblazor:categories                 → Category mapping
mudblazor:api:{typeName}             → API reference types
```

---

## 6. Configuration

### appsettings.json

```json
{
  "MudBlazor": {
    "Repository": {
      "Url": "https://github.com/MudBlazor/MudBlazor.git",
      "Branch": "dev",
      "LocalPath": "./data/mudblazor-repo"
    },
    "Cache": {
      "RefreshIntervalMinutes": 60,
      "ComponentCacheDurationMinutes": 30,
      "ExampleCacheDurationMinutes": 120
    },
    "Parsing": {
      "IncludeInternalComponents": false,
      "IncludeDeprecatedComponents": true,
      "MaxExamplesPerComponent": 20
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MudBlazor.Mcp": "Debug"
    }
  }
}
```

---

## 7. Implementation Phases

### Phase 1: Foundation (Week 1)
1. Create solution structure with all projects
2. Implement `GitRepositoryService` for cloning/updating repo
3. Set up Aspire orchestration
4. Add basic health checks and logging

### Phase 2: Parsing (Week 2)
1. Implement `XmlDocParser` for C# XML comments
2. Implement `RazorFileParser` for documentation pages
3. Implement `ExampleExtractor` for code examples
4. Implement `CategoryMapper` from MenuService

### Phase 3: Core Tools (Week 3)
1. Implement `list_components` tool
2. Implement `get_component_detail` tool
3. Implement `search_components` tool
4. Implement `get_components_by_category` tool

### Phase 4: Advanced Tools (Week 4)
1. Implement `get_component_examples` tool
2. Implement `get_component_parameters` tool
3. Implement `get_api_reference` tool
4. Implement `get_related_components` tool

### Phase 5: Polish & Testing (Week 5)
1. Add comprehensive unit tests
2. Add integration tests with actual MudBlazor repo
3. Performance optimization
4. Documentation and README

---

## 8. Technical Decisions

### Q1: Tool Naming Convention
**Decision**: Use `snake_case` verb-noun pattern
- `list_components` instead of `GetComponentsList`
- `get_component_detail` instead of `ComponentDetail`
- Aligns with MCP specification best practices

### Q2: Parsing XML Doc Comments
**Decision**: Use Roslyn for robust parsing
- Handles complex generic types
- Resolves `<see cref="">` links properly
- Extracts `[Category]` and other attributes

### Q3: Component Inheritance
**Decision**: Track inheritance chain and allow filtered views
- Store `BaseType` for each component
- `includeInheritedMembers` parameter in tools
- `MudComponentBase` → `MudBaseButton` → `MudButton` chain preserved

### Q4: Caching Strategy
**Decision**: Memory cache with optional distributed cache
- Fast local access for single-instance deployments
- IDistributedCache interface for future scaling
- Background refresh to keep data fresh

### Q5: Include MudBlazor.Docs Examples
**Decision**: Yes, include documentation examples
- Rich code examples improve AI understanding
- Associate examples with specific documentation sections
- Parse both inline examples and separate `*Example.razor` files

---

## 9. Testing Strategy

### Unit Tests
- Parser tests with sample C# and Razor files
- Tool output format validation
- Cache behavior tests

### Integration Tests
- Full parsing of actual MudBlazor repository
- Tool response validation against known components
- Performance benchmarks

### Test Data
- Create sample component files for unit tests
- Use actual MudBlazor repo for integration tests
- Verify against MudBlazor docs website for accuracy

---

## 10. Deployment Considerations

### Local Development
```bash
# Clone the MCP server
git clone https://github.com/mcbodge/MudBlazor.Mcp.git
cd MudBlazor.Mcp

# Run with Aspire
dotnet run --project src/MudBlazor.Mcp.AppHost

# Or run standalone
dotnet run --project src/MudBlazor.Mcp
```

### Production
- Container-ready with Dockerfile
- Health endpoints for monitoring
- OpenTelemetry for observability
- Configurable via environment variables

---

## Next Steps

1. Review and approve this implementation plan
2. Set up the initial solution structure
3. Begin Phase 1 implementation
4. Regular progress updates and adjustments

---

*Document Version: 1.0*
*Created: December 18, 2025*
