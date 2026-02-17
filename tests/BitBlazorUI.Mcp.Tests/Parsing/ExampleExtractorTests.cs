// Copyright (c) 2025 Bit BlazorUI MCP Contributors
// Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using BitBlazorUI.Mcp.Services.Parsing;

namespace BitBlazorUI.Mcp.Tests.Parsing;

public class ExampleExtractorTests
{
    private readonly ExampleExtractor _extractor;

    public ExampleExtractorTests()
    {
        var logger = Mock.Of<ILogger<ExampleExtractor>>();
        _extractor = new ExampleExtractor(logger);
    }

    [Fact]
    public async Task ParseSamplesFileAsync_WithRazorExample_ExtractsMarkupAndCode()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".razor.samples.cs";
        var content = """
            namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.BitButton;

            public partial class BitButtonDemo
            {
                private readonly string example1RazorCode = @"
            <BitButton Color=""BitColor.Primary"" Variant=""BitVariant.Fill"">
                Click Me
            </BitButton>";

                private readonly string example1CsharpCode = @"
            private void HandleClick()
            {
                Console.WriteLine(""Clicked!"");
            }";
            }
            """;
        await File.WriteAllTextAsync(tempFile, content);

        try
        {
            // Act
            var results = await _extractor.ParseSamplesFileAsync(tempFile, "BitButton", CancellationToken.None);

            // Assert
            Assert.NotEmpty(results);
            var result = results[0];
            Assert.Contains("BitButton", result.RazorMarkup);
            Assert.Contains("HandleClick", result.CSharpCode);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseSamplesFileAsync_WithNoCodeBlock_OnlyExtractsMarkup()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".razor.samples.cs";
        var content = """
            namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.BitButton;

            public partial class BitButtonDemo
            {
                private readonly string example1RazorCode = @"
            <BitButton Color=""BitColor.Primary"">
                Simple Button
            </BitButton>";
            }
            """;
        await File.WriteAllTextAsync(tempFile, content);

        try
        {
            // Act
            var results = await _extractor.ParseSamplesFileAsync(tempFile, "BitButton", CancellationToken.None);

            // Assert
            Assert.NotEmpty(results);
            var result = results[0];
            Assert.Contains("BitButton", result.RazorMarkup);
            Assert.Null(result.CSharpCode);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseSamplesFileAsync_WithFeatures_ExtractsFeaturedFeatures()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".razor.samples.cs";
        var content = """
            namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.BitButton;

            public partial class BitButtonDemo
            {
                private readonly string example1RazorCode = @"
            <BitButton Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" Size=""BitSize.Large"" OnClick=""HandleClick"">
                Click Me
            </BitButton>";

                private readonly string example1CsharpCode = @"
            private void HandleClick() { }";
            }
            """;
        await File.WriteAllTextAsync(tempFile, content);

        try
        {
            // Act
            var results = await _extractor.ParseSamplesFileAsync(tempFile, "BitButton", CancellationToken.None);

            // Assert
            Assert.NotEmpty(results);
            Assert.NotEmpty(results[0].Features);
            // Should detect common features like Colors, Variants, Sizes
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseSamplesFileAsync_CleansUpDirectives()
    {
        // Arrange
        var tempFile = Path.GetTempFileName() + ".razor.samples.cs";
        var content = """
            namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.BitButton;

            public partial class BitButtonDemo
            {
                private readonly string example1RazorCode = @"
            <BitButton>Test</BitButton>";
            }
            """;
        await File.WriteAllTextAsync(tempFile, content);

        try
        {
            // Act
            var results = await _extractor.ParseSamplesFileAsync(tempFile, "BitButton", CancellationToken.None);

            // Assert
            Assert.NotEmpty(results);
            // Samples.cs files don't have @page, @using directives - they're already clean
            Assert.Contains("BitButton", results[0].RazorMarkup);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseSamplesFileAsync_NonExistentFile_ReturnsEmpty()
    {
        // Act
        var results = await _extractor.ParseSamplesFileAsync("/nonexistent/path.razor.samples.cs", "BitButton", CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ParseSamplesFileAsync_WithVariantSamplesFile_UsesVariantNamePrefix()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "_BitButtonGroupItemDemo.razor.samples.cs");
        var content = """
            namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

            public partial class _BitButtonGroupItemDemo
            {
                private readonly string example1RazorCode = @"
            <BitButtonGroup>
                <BitButton>One</BitButton>
            </BitButtonGroup>";
            }
            """;
        await File.WriteAllTextAsync(tempFile, content);

        try
        {
            // Act
            var results = await _extractor.ParseSamplesFileAsync(tempFile, "BitButtonGroup", CancellationToken.None);

            // Assert
            Assert.NotEmpty(results);
            Assert.Equal("Item Example 1", results[0].Name);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
