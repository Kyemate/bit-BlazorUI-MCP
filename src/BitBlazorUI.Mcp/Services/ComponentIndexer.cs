// Copyright (c) 2025 Bit BlazorUI MCP Contributors
// Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BitBlazorUI.Mcp.Configuration;
using BitBlazorUI.Mcp.Models;
using BitBlazorUI.Mcp.Services.Parsing;

namespace BitBlazorUI.Mcp.Services;

/// <summary>
/// Indexes and queries Bit BlazorUI component documentation.
/// </summary>
public sealed class ComponentIndexer : IComponentIndexer
{
    private readonly IGitRepositoryService _gitService;
    private readonly IDocumentationCache _cache;
    private readonly XmlDocParser _xmlParser;
    private readonly RazorDocParser _razorParser;
    private readonly ExampleExtractor _exampleExtractor;
    private readonly CategoryMapper _categoryMapper;
    private readonly ILogger<ComponentIndexer> _logger;
    private readonly BitBlazorUIOptions _options;

    private readonly ConcurrentDictionary<string, ComponentInfo> _components = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ApiReference> _apiReferences = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _indexLock = new(1, 1);

    private bool _isIndexed;
    private DateTimeOffset? _lastIndexed;

    public bool IsIndexed => _isIndexed;
    public DateTimeOffset? LastIndexed => _lastIndexed;

    public ComponentIndexer(
        IGitRepositoryService gitService,
        IDocumentationCache cache,
        XmlDocParser xmlParser,
        RazorDocParser razorParser,
        ExampleExtractor exampleExtractor,
        CategoryMapper categoryMapper,
        IOptions<BitBlazorUIOptions> options,
        ILogger<ComponentIndexer> logger)
    {
        _gitService = gitService;
        _cache = cache;
        _xmlParser = xmlParser;
        _razorParser = razorParser;
        _exampleExtractor = exampleExtractor;
        _categoryMapper = categoryMapper;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task BuildIndexAsync(CancellationToken cancellationToken = default)
    {
        await _indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("Starting index build...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // Ensure we have the repository
            await _gitService.EnsureRepositoryAsync(cancellationToken).ConfigureAwait(false);

            if (!_gitService.IsAvailable)
            {
                throw new InvalidOperationException("Repository is not available for indexing");
            }

            var repoPath = _gitService.RepositoryPath!;

            // Initialize category mapper
            await _categoryMapper.InitializeAsync(repoPath, cancellationToken).ConfigureAwait(false);

            // Index components
            await IndexComponentsAsync(repoPath, cancellationToken).ConfigureAwait(false);

            // Index documentation
            await IndexDocumentationAsync(repoPath, cancellationToken).ConfigureAwait(false);

            // Index examples
            await IndexExamplesAsync(repoPath, cancellationToken).ConfigureAwait(false);

            _isIndexed = true;
            _lastIndexed = DateTimeOffset.UtcNow;

            sw.Stop();
            _logger.LogInformation("Index build completed in {ElapsedMs}ms. Indexed {Count} components",
                sw.ElapsedMilliseconds, _components.Count);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    private async Task IndexComponentsAsync(string repoPath, CancellationToken cancellationToken)
    {
        var componentRoots = new[]
        {
            Path.Combine(repoPath, "src", "BlazorUI", "Bit.BlazorUI", "Components"),
            Path.Combine(repoPath, "src", "BlazorUI", "Bit.BlazorUI.Extras", "Components")
        };

        var componentDirs = new List<string>();

        foreach (var componentsPath in componentRoots)
        {
            if (!Directory.Exists(componentsPath))
            {
                _logger.LogDebug("Components directory not found: {Path}", componentsPath);
                continue;
            }

            // Bit.BlazorUI uses nested dirs: Components/{Category}/{ComponentName}/
            // Bit.BlazorUI.Extras may use flat dirs: Components/{ComponentName}/
            foreach (var firstLevelDir in Directory.GetDirectories(componentsPath))
            {
                if (ContainsMainComponentFile(firstLevelDir))
                {
                    componentDirs.Add(firstLevelDir);
                }
                else
                {
                    componentDirs.AddRange(Directory.GetDirectories(firstLevelDir));
                }
            }
        }

        var uniqueComponentDirs = componentDirs
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogDebug("Found {Count} component directories", uniqueComponentDirs.Count);

        var tasks = uniqueComponentDirs.Select(dir => IndexComponentDirectoryAsync(repoPath, dir, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task IndexComponentDirectoryAsync(string repoPath, string componentDir, CancellationToken cancellationToken)
    {
        var dirName = Path.GetFileName(componentDir);

        // Find the main component file (e.g., BitButton.razor.cs or BitButton.cs)
        var razorCsFile = Directory.GetFiles(componentDir, "Bit*.razor.cs").FirstOrDefault();
        var csFile = Directory.GetFiles(componentDir, "Bit*.cs")
            .FirstOrDefault(f => !f.EndsWith(".razor.cs"));

        var mainFile = razorCsFile ?? csFile;

        if (mainFile is null)
        {
            _logger.LogDebug("No main component file found in: {Dir}", dirName);
            return;
        }

        try
        {
            var parseResult = await _xmlParser.ParseComponentFileAsync(mainFile, cancellationToken).ConfigureAwait(false);

            if (parseResult is null)
            {
                return;
            }

            var componentName = parseResult.ClassName;
            var category = _categoryMapper.GetCategoryName(componentName)
                ?? _categoryMapper.InferCategoryFromName(componentName);

            var sourceRelativePath = Path.GetRelativePath(repoPath, componentDir).Replace('\\', '/');

            var componentInfo = new ComponentInfo(
                Name: componentName,
                Namespace: parseResult.Namespace ?? "Bit.BlazorUI",
                Summary: parseResult.Summary ?? $"{componentName} component",
                Description: parseResult.Remarks,
                Category: category,
                BaseType: parseResult.BaseType,
                Parameters: parseResult.Parameters,
                Events: parseResult.Events,
                Methods: parseResult.Methods,
                Examples: [],
                RelatedComponents: [],
                DocumentationUrl: $"https://blazorui.bitplatform.dev/components/{dirName.ToLowerInvariant()}",
                SourceUrl: $"https://github.com/bitfoundation/bitplatform/tree/main/{sourceRelativePath}"
            );

            _components[componentName] = componentInfo;
            _logger.LogTrace("Indexed component: {Name}", componentName);

            // Also index as API reference
            _apiReferences[componentName] = CreateApiReference(parseResult);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "IO error indexing component in: {Dir}", dirName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied indexing component in: {Dir}", dirName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to index component in: {Dir}", dirName);
        }
    }

    private static bool ContainsMainComponentFile(string directory)
    {
        var razorCsFile = Directory.GetFiles(directory, "Bit*.razor.cs").FirstOrDefault();
        if (razorCsFile is not null)
        {
            return true;
        }

        var csFile = Directory.GetFiles(directory, "Bit*.cs")
            .FirstOrDefault(f => !f.EndsWith(".razor.cs", StringComparison.OrdinalIgnoreCase));

        return csFile is not null;
    }

    private static ApiReference CreateApiReference(ComponentParseResult parseResult)
    {
        var members = new List<ApiMember>();

        // Add parameters as properties
        foreach (var param in parseResult.Parameters)
        {
            members.Add(new ApiMember(
                Name: param.Name,
                MemberType: "Property",
                ReturnType: param.Type,
                Description: param.Description
            ));
        }

        // Add events
        foreach (var evt in parseResult.Events)
        {
            members.Add(new ApiMember(
                Name: evt.Name,
                MemberType: "Event",
                ReturnType: evt.EventArgsType is not null
                    ? $"EventCallback<{evt.EventArgsType}>"
                    : "EventCallback",
                Description: evt.Description
            ));
        }

        // Add methods
        foreach (var method in parseResult.Methods)
        {
            var paramSignature = method.Parameters.Count > 0
                ? string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"))
                : null;

            members.Add(new ApiMember(
                Name: method.Name,
                MemberType: "Method",
                ReturnType: method.ReturnType,
                Description: method.Description,
                ParameterSignature: paramSignature
            ));
        }

        return new ApiReference(
            TypeName: parseResult.ClassName,
            Namespace: parseResult.Namespace ?? "Bit.BlazorUI",
            Summary: parseResult.Summary,
            BaseType: parseResult.BaseType,
            Members: members
        );
    }

    private async Task IndexDocumentationAsync(string repoPath, CancellationToken cancellationToken)
    {
        var docsPath = Path.Combine(repoPath, "src", "BlazorUI", "Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components");

        if (!Directory.Exists(docsPath))
        {
            _logger.LogWarning("Documentation directory not found: {Path}", docsPath);
            return;
        }

        var docFiles = Directory.GetFiles(docsPath, "*Demo.razor", SearchOption.AllDirectories);
        _logger.LogDebug("Found {Count} documentation files", docFiles.Length);

        foreach (var docFile in docFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var docResult = await _razorParser.ParseDocumentationFileAsync(docFile, cancellationToken).ConfigureAwait(false);

                if (docResult?.ComponentName is not null && _components.TryGetValue(docResult.ComponentName, out var component))
                {
                    // Enhance component with documentation info
                    var enhanced = component with
                    {
                        Description = docResult.Description ?? component.Description,
                        RelatedComponents = docResult.RelatedComponents
                    };

                    _components[docResult.ComponentName] = enhanced;
                }
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "IO error parsing documentation file: {File}", docFile);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied parsing documentation file: {File}", docFile);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to parse documentation file: {File}", docFile);
            }
        }
    }

    private async Task IndexExamplesAsync(string repoPath, CancellationToken cancellationToken)
    {
        var docsPath = repoPath;

        foreach (var (componentName, component) in _components)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var examples = await _exampleExtractor.ExtractExamplesAsync(docsPath, componentName, cancellationToken).ConfigureAwait(false);

                if (examples.Count > 0)
                {
                    var enhanced = component with { Examples = examples };
                    _components[componentName] = enhanced;
                }
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "IO error extracting examples for: {Component}", componentName);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied extracting examples for: {Component}", componentName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to extract examples for: {Component}", componentName);
            }
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComponentInfo>> GetAllComponentsAsync(CancellationToken cancellationToken = default)
    {
        EnsureIndexed();
        return Task.FromResult<IReadOnlyList<ComponentInfo>>(_components.Values.ToList());
    }

    /// <inheritdoc />
    public Task<ComponentInfo?> GetComponentAsync(string componentName, CancellationToken cancellationToken = default)
    {
        EnsureIndexed();

        // Try exact match first
        if (_components.TryGetValue(componentName, out var component))
        {
            return Task.FromResult<ComponentInfo?>(component);
        }

        // Try with "Bit" prefix
        if (!componentName.StartsWith("Bit", StringComparison.OrdinalIgnoreCase))
        {
            if (_components.TryGetValue($"Bit{componentName}", out component))
            {
                return Task.FromResult<ComponentInfo?>(component);
            }
        }

        return Task.FromResult<ComponentInfo?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComponentCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        EnsureIndexed();
        return Task.FromResult<IReadOnlyList<ComponentCategory>>(_categoryMapper.GetCategories().ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComponentInfo>> GetComponentsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        EnsureIndexed();

        var components = _components.Values
            .Where(c => c.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        return Task.FromResult<IReadOnlyList<ComponentInfo>>(components);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComponentInfo>> SearchComponentsAsync(
        string query,
        SearchFields searchFields = SearchFields.All,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        EnsureIndexed();

        var queryLower = query.ToLowerInvariant();
        var results = new List<(ComponentInfo Component, int Score)>();

        foreach (var component in _components.Values)
        {
            var score = CalculateSearchScore(component, queryLower, searchFields);
            if (score > 0)
            {
                results.Add((component, score));
            }
        }

        var sorted = results
            .OrderByDescending(r => r.Score)
            .Take(maxResults)
            .Select(r => r.Component)
            .ToList();

        return Task.FromResult<IReadOnlyList<ComponentInfo>>(sorted);
    }

    private static int CalculateSearchScore(ComponentInfo component, string query, SearchFields fields)
    {
        var score = 0;

        if (fields.HasFlag(SearchFields.Name))
        {
            if (component.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                score += component.Name.Equals(query, StringComparison.OrdinalIgnoreCase) ? 100 : 50;
            }
        }

        if (fields.HasFlag(SearchFields.Description))
        {
            if (component.Summary?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                score += 30;
            if (component.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                score += 20;
        }

        if (fields.HasFlag(SearchFields.Parameters))
        {
            foreach (var param in component.Parameters)
            {
                if (param.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score += 10;
                if (param.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                    score += 5;
            }
        }

        if (fields.HasFlag(SearchFields.Examples))
        {
            foreach (var example in component.Examples)
            {
                if (example.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    score += 5;
            }
        }

        return score;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComponentExample>> GetExamplesAsync(string componentName, CancellationToken cancellationToken = default)
    {
        EnsureIndexed();

        if (_components.TryGetValue(componentName, out var component))
        {
            return Task.FromResult<IReadOnlyList<ComponentExample>>(component.Examples);
        }

        return Task.FromResult<IReadOnlyList<ComponentExample>>([]);
    }

    /// <inheritdoc />
    public Task<ApiReference?> GetApiReferenceAsync(string typeName, CancellationToken cancellationToken = default)
    {
        EnsureIndexed();

        if (_apiReferences.TryGetValue(typeName, out var apiRef))
        {
            return Task.FromResult<ApiReference?>(apiRef);
        }

        // Try with Bit prefix
        if (!typeName.StartsWith("Bit", StringComparison.OrdinalIgnoreCase))
        {
            if (_apiReferences.TryGetValue($"Bit{typeName}", out apiRef))
            {
                return Task.FromResult<ApiReference?>(apiRef);
            }
        }

        return Task.FromResult<ApiReference?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ComponentInfo>> GetRelatedComponentsAsync(
        string componentName,
        RelationshipType relationshipType = RelationshipType.All,
        CancellationToken cancellationToken = default)
    {
        EnsureIndexed();

        if (!_components.TryGetValue(componentName, out var component))
        {
            return Task.FromResult<IReadOnlyList<ComponentInfo>>([]);
        }

        var related = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add explicitly related components
        foreach (var relatedName in component.RelatedComponents)
        {
            related.Add(relatedName);
        }

        // Add components in same category (siblings)
        if (relationshipType is RelationshipType.All or RelationshipType.Sibling)
        {
            if (component.Category is not null)
            {
                var categoryComponents = _categoryMapper.GetComponentsInCategory(component.Category);
                foreach (var cat in categoryComponents.Where(c => !c.Equals(componentName, StringComparison.OrdinalIgnoreCase)))
                {
                    related.Add(cat);
                }
            }
        }

        // Add parent (base type)
        if (relationshipType is RelationshipType.All or RelationshipType.Parent)
        {
            if (component.BaseType is not null && _components.ContainsKey(component.BaseType))
            {
                related.Add(component.BaseType);
            }
        }

        // Add children (components that inherit from this one)
        if (relationshipType is RelationshipType.All or RelationshipType.Child)
        {
            foreach (var (name, comp) in _components)
            {
                if (comp.BaseType?.Equals(componentName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    related.Add(name);
                }
            }
        }

        var relatedComponents = related
            .Where(r => _components.ContainsKey(r))
            .Select(r => _components[r])
            .Take(10)
            .ToList();

        return Task.FromResult<IReadOnlyList<ComponentInfo>>(relatedComponents);
    }

    private void EnsureIndexed()
    {
        if (!_isIndexed)
        {
            throw new InvalidOperationException("Index has not been built. Call BuildIndexAsync first.");
        }
    }
}
