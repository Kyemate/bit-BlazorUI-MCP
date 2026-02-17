// Copyright (c) 2025 Bit BlazorUI MCP Contributors
// Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using BitBlazorUI.Mcp.Models;

namespace BitBlazorUI.Mcp.Services.Parsing;

/// <summary>
/// Extracts code examples from Bit BlazorUI demo sample files.
/// </summary>
public sealed partial class ExampleExtractor
{
    private readonly ILogger<ExampleExtractor> _logger;

    public ExampleExtractor(ILogger<ExampleExtractor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts all examples for a component from the demo folder.
    /// </summary>
    /// <param name="docsPath">The path to the demo repository root folder.</param>
    /// <param name="componentName">The component name to extract examples for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of component examples.</returns>
    public async Task<List<ComponentExample>> ExtractExamplesAsync(
        string docsPath,
        string componentName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(docsPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);

        var examples = new List<ComponentExample>();

        // Component examples are in: src/BlazorUI/Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/{Category}/{ComponentName}/Bit{ComponentName}Demo.razor.samples.cs
        // The folder name is usually the component name without "Bit" prefix
        var folderName = componentName.StartsWith("Bit") ? componentName[3..] : componentName;
        var componentsPath = Path.Combine(docsPath, "src", "BlazorUI", "Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components");

        if (!Directory.Exists(componentsPath))
        {
            _logger.LogDebug("No components folder found at {Path}", componentsPath);
            return examples;
        }

        // Bit BlazorUI stores examples in three possible locations:
        // 1. BitXxxDemo.razor.samples.cs (dedicated samples file)
        // 2. BitXxxDemo.razor.cs (examples inline in demo code-behind)
        // 3. _BitXxx{Item,Option,Custom}Demo.razor.samples.cs (variant samples)
        List<string> filesToParse;
        try
        {
            filesToParse = FindExampleFiles(componentsPath, folderName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied to components folder: {Path}", componentsPath);
            return examples;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "IO error accessing components folder: {Path}", componentsPath);
            return examples;
        }

        if (filesToParse.Count == 0)
        {
            _logger.LogDebug("No example files found for component {ComponentName}", componentName);
            return examples;
        }

        foreach (var filePath in filesToParse)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var parsedExamples = await ParseSamplesFileAsync(filePath, componentName, cancellationToken).ConfigureAwait(false);
                examples.AddRange(parsedExamples);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "IO error reading file: {FilePath}", filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied reading file: {FilePath}", filePath);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to parse file: {FilePath}", filePath);
            }
        }

        _logger.LogDebug("Found {Count} examples for {ComponentName}", examples.Count, componentName);
        return examples;
    }

    /// <summary>
    /// Finds all files containing example code for a component.
    /// </summary>
    private List<string> FindExampleFiles(string componentsPath, string folderName)
    {
        var files = new List<string>();

        // 1. Dedicated samples file: BitXxxDemo.razor.samples.cs
        var samplesPattern = $"Bit{folderName}Demo.razor.samples.cs";
        var samplesFiles = Directory.GetFiles(componentsPath, samplesPattern, SearchOption.AllDirectories);
        files.AddRange(samplesFiles);

        // 2. Variant samples: _BitXxx{Item,Option,Custom}Demo.razor.samples.cs
        var variantPattern = $"_Bit{folderName}*Demo.razor.samples.cs";
        var variantFiles = Directory.GetFiles(componentsPath, variantPattern, SearchOption.AllDirectories);
        files.AddRange(variantFiles);

        // 3. Fallback: examples inline in BitXxxDemo.razor.cs (only if no .samples.cs found)
        if (files.Count == 0)
        {
            var codePattern = $"Bit{folderName}Demo.razor.cs";
            var codeFiles = Directory.GetFiles(componentsPath, codePattern, SearchOption.AllDirectories);
            files.AddRange(codeFiles);
        }

        return files;
    }

    /// <summary>
    /// Parses a samples file containing inline string constants.
    /// </summary>
    /// <param name="filePath">The path to the samples file.</param>
    /// <param name="componentName">The component name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of parsed component examples.</returns>
    public async Task<List<ComponentExample>> ParseSamplesFileAsync(
        string filePath,
        string componentName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);

        var examples = new List<ComponentExample>();

        if (!File.Exists(filePath))
            return examples;

        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        var fileName = Path.GetFileName(filePath);

        // Extract all razor code fields
        var razorMatches = RazorCodeFieldRegex().Matches(content);
        var razorCodes = new Dictionary<int, string>();
        foreach (Match match in razorMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out var exampleNumber))
            {
                razorCodes[exampleNumber] = match.Groups[2].Value;
            }
        }

        // Extract all C# code fields
        var csharpMatches = CSharpCodeFieldRegex().Matches(content);
        var csharpCodes = new Dictionary<int, string>();
        foreach (Match match in csharpMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out var exampleNumber))
            {
                csharpCodes[exampleNumber] = match.Groups[2].Value;
            }
        }

        // Get all unique example numbers
        var exampleNumbers = razorCodes.Keys.Union(csharpCodes.Keys).OrderBy(n => n);

        foreach (var exampleNumber in exampleNumbers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var razorMarkup = razorCodes.GetValueOrDefault(exampleNumber);
            var csharpCode = csharpCodes.GetValueOrDefault(exampleNumber);

            // Clean up the markup
            if (!string.IsNullOrWhiteSpace(razorMarkup))
            {
                razorMarkup = CleanMarkup(razorMarkup);
            }

            // Extract features from the combined content
            var combinedContent = (razorMarkup ?? "") + "\n" + (csharpCode ?? "");
            var features = ExtractFeaturedFeatures(combinedContent);

            examples.Add(new ComponentExample(
                Name: $"Example {exampleNumber}",
                Description: null,
                RazorMarkup: razorMarkup,
                CSharpCode: csharpCode,
                SourceFile: fileName,
                Features: features
            ));
        }

        return examples;
    }

    private static string PascalCaseToSpaces(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return PascalCaseRegex().Replace(text, " $1").Trim();
    }

    private static string CleanMarkup(string markup)
    {
        // Remove @page directive
        markup = PageDirectiveRegex().Replace(markup, "");

        // Remove @using statements
        markup = UsingDirectiveRegex().Replace(markup, "");

        // Remove @inject statements
        markup = InjectDirectiveRegex().Replace(markup, "");

        // Remove @namespace
        markup = NamespaceDirectiveRegex().Replace(markup, "");

        // Clean up extra blank lines
        markup = MultipleNewlinesRegex().Replace(markup, "\n\n");

        return markup.Trim();
    }

    private static List<string> ExtractFeaturedFeatures(string content)
    {
        var features = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Extract parameter names used in the example
        var paramMatches = ParameterUsageRegex().Matches(content);
        foreach (Match match in paramMatches)
        {
            features.Add(match.Groups[1].Value);
        }

        // Extract commonly highlighted features
        if (content.Contains("@bind", StringComparison.OrdinalIgnoreCase))
            features.Add("Two-way binding");

        if (content.Contains("EventCallback") || content.Contains("@on"))
            features.Add("Event handling");

        if (content.Contains("Variant="))
            features.Add("Variants");

        if (content.Contains("Color="))
            features.Add("Colors");

        if (content.Contains("Size="))
            features.Add("Sizes");

        return features.Take(5).ToList();
    }

    [GeneratedRegex(@"(?<!^)([A-Z])")]
    private static partial Regex PascalCaseRegex();

    [GeneratedRegex(@"private readonly string example(\d+)RazorCode\s*=\s*@""(.*?)"";", RegexOptions.Singleline)]
    private static partial Regex RazorCodeFieldRegex();

    [GeneratedRegex(@"private readonly string example(\d+)CsharpCode\s*=\s*@""(.*?)"";", RegexOptions.Singleline)]
    private static partial Regex CSharpCodeFieldRegex();

    [GeneratedRegex(@"@page\s+""[^""]*""\s*\n?")]
    private static partial Regex PageDirectiveRegex();

    [GeneratedRegex(@"@using\s+[^\n]+\n?")]
    private static partial Regex UsingDirectiveRegex();

    [GeneratedRegex(@"@inject\s+[^\n]+\n?")]
    private static partial Regex InjectDirectiveRegex();

    [GeneratedRegex(@"@namespace\s+[^\n]+\n?")]
    private static partial Regex NamespaceDirectiveRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultipleNewlinesRegex();

    [GeneratedRegex(@"(\w+)=""[^""]*""")]
    private static partial Regex ParameterUsageRegex();
}
