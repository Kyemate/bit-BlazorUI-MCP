// Copyright (c) 2025 Bit BlazorUI MCP Contributors
// Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using BitBlazorUI.Mcp.Models;

namespace BitBlazorUI.Mcp.Services.Parsing;

/// <summary>
/// Parses Razor documentation files to extract component descriptions and sections for Bit BlazorUI components.
/// </summary>
public sealed partial class RazorDocParser
{
    private readonly ILogger<RazorDocParser> _logger;

    public RazorDocParser(ILogger<RazorDocParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses a Razor documentation file for component documentation.
    /// </summary>
    /// <param name="filePath">The path to the Razor documentation file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parse result, or null if the file doesn't exist or parsing failed.</returns>
    public async Task<RazorDocResult?> ParseDocumentationFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Documentation file not found: {FilePath}", filePath);
            return null;
        }

        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
            return ParseDocumentation(content, filePath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error reading documentation file: {FilePath}", filePath);
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied reading documentation file: {FilePath}", filePath);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to parse documentation file: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Parses Razor documentation content.
    /// </summary>
    /// <param name="content">The Razor file content.</param>
    /// <param name="filePath">The file path for reference.</param>
    /// <returns>The parsed documentation result.</returns>
    public RazorDocResult? ParseDocumentation(string content, string filePath)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var componentName = ExtractComponentName(filePath);
        var title = ExtractPageTitle(content);
        var description = ExtractSubTitle(content);
        var sections = ExtractSections(content);
        var relatedComponents = ExtractRelatedComponents(content);
        var usageNotes = ExtractUsageNotes(content);

        return new RazorDocResult
        {
            FilePath = filePath,
            ComponentName = componentName,
            Title = title,
            Description = description,
            Sections = sections,
            RelatedComponents = relatedComponents,
            UsageNotes = usageNotes
        };
    }

    private static string? ExtractComponentName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // Handle pattern like "BitButtonDemo" -> "BitButton"
        if (fileName.EndsWith("Demo"))
        {
            var name = fileName[..^4]; // Remove "Demo"
            return name; // Already has Bit prefix (e.g., "BitButton")
        }

        return null;
    }

    private static string? ExtractPageTitle(string content)
    {
        // Match: <DocsPage title="Button">
        var match = TitleAttributeRegex().Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractSubTitle(string content)
    {
        // Match: <DocsPageHeader Title="..." SubTitle="...">
        var match = SubTitleAttributeRegex().Match(content);
        if (match.Success)
            return match.Groups[1].Value;

        // Alternative: <BitText> in header section
        match = HeaderTextRegex().Match(content);
        return match.Success ? match.Groups[1].Value : null;
    }

    private List<DocumentationSection> ExtractSections(string content)
    {
        var sections = new List<DocumentationSection>();

        // Match DocsPageSection components
        var sectionMatches = DocsSectionRegex().Matches(content);

        foreach (Match match in sectionMatches)
        {
            var title = match.Groups[1].Value;
            var sectionContent = match.Groups[2].Value;

            sections.Add(new DocumentationSection
            {
                Title = title,
                Content = ExtractTextContent(sectionContent),
                HasExample = sectionContent.Contains("SectionSource") ||
                            sectionContent.Contains("Example")
            });
        }

        return sections;
    }

    private static string ExtractTextContent(string razorContent)
    {
        // Remove Razor markup and extract readable text
        var text = razorContent;

        // Remove component tags
        text = ComponentTagRegex().Replace(text, "");

        // Remove code blocks
        text = CodeBlockRegex().Replace(text, "");

        // Remove @ directives
        text = DirectiveRegex().Replace(text, "");

        // Clean up whitespace
        text = WhitespaceRegex().Replace(text, " ");

        return text.Trim();
    }

    private static List<string> ExtractRelatedComponents(string content)
    {
        var related = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Find links to other components
        var linkMatches = ComponentLinkRegex().Matches(content);
        foreach (Match match in linkMatches)
        {
            var componentPath = match.Groups[1].Value;
            var componentName = Path.GetFileNameWithoutExtension(componentPath);

            if (componentName.EndsWith("Demo"))
            {
                var name = componentName[..^4]; // Remove "Demo", already has Bit prefix
                related.Add(name);
            }
        }

        // Find BitXxx references in code
        var bitMatches = BitComponentRefRegex().Matches(content);
        foreach (Match match in bitMatches)
        {
            related.Add(match.Groups[1].Value);
        }

        return related.ToList();
    }

    private static List<string> ExtractUsageNotes(string content)
    {
        var notes = new List<string>();

        // Extract BitMessageBar content (often used for important notes)
        var alertMatches = AlertContentRegex().Matches(content);
        foreach (Match match in alertMatches)
        {
            var alertText = ExtractTextContent(match.Groups[1].Value);
            if (!string.IsNullOrWhiteSpace(alertText))
            {
                notes.Add(alertText);
            }
        }

        return notes;
    }

    [GeneratedRegex(@"Title\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex TitleAttributeRegex();

    [GeneratedRegex(@"SubTitle\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex SubTitleAttributeRegex();

    [GeneratedRegex(@"<BitText[^>]*>\s*([^<]+)\s*</BitText>")]
    private static partial Regex HeaderTextRegex();

    [GeneratedRegex(@"<DocsPageSection\s+Title\s*=\s*""([^""]+)""[^>]*>(.*?)</DocsPageSection>", RegexOptions.Singleline)]
    private static partial Regex DocsSectionRegex();

    [GeneratedRegex(@"<[A-Z][^>]*>.*?</[A-Z][^>]*>|<[A-Z][^/]*/>", RegexOptions.Singleline)]
    private static partial Regex ComponentTagRegex();

    [GeneratedRegex(@"<code>.*?</code>|@\{.*?\}", RegexOptions.Singleline)]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"@[a-zA-Z]+(\.[a-zA-Z]+)*")]
    private static partial Regex DirectiveRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"href\s*=\s*""/components/([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex ComponentLinkRegex();

    [GeneratedRegex(@"<(Bit[A-Z][a-zA-Z]+)")]
    private static partial Regex BitComponentRefRegex();

    [GeneratedRegex(@"<BitMessageBar[^>]*>(.*?)</BitMessageBar>", RegexOptions.Singleline)]
    private static partial Regex AlertContentRegex();
}

/// <summary>
/// Result of parsing a Razor documentation file.
/// </summary>
public record RazorDocResult
{
    public required string FilePath { get; init; }
    public string? ComponentName { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public List<DocumentationSection> Sections { get; init; } = [];
    public List<string> RelatedComponents { get; init; } = [];
    public List<string> UsageNotes { get; init; } = [];
}

/// <summary>
/// A section in the documentation.
/// </summary>
public record DocumentationSection
{
    public required string Title { get; init; }
    public string? Content { get; init; }
    public bool HasExample { get; init; }
}
