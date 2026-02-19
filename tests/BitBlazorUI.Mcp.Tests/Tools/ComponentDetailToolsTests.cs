// Copyright (c) 2025 Bit BlazorUI MCP Contributors
// Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BitBlazorUI.Mcp.Models;
using BitBlazorUI.Mcp.Services;
using BitBlazorUI.Mcp.Tools;

namespace BitBlazorUI.Mcp.Tests.Tools;

public class ComponentDetailToolsTests
{
    private static readonly ILogger<ComponentDetailTools> NullLogger =
        NullLoggerFactory.Instance.CreateLogger<ComponentDetailTools>();

    [Fact]
    public async Task GetComponentDetailAsync_WithValidComponent_ReturnsDetails()
    {
        // Arrange
        var indexer = CreateMockIndexer();

        // Act
        var result = await ComponentDetailTools.GetComponentDetailAsync(
            indexer, NullLogger, "BitButton", false, true, CancellationToken.None);

        // Assert
        Assert.Contains("BitButton", result);
        Assert.Contains("A button component", result);
        Assert.Contains("Parameters", result);
        Assert.Contains("Color", result);
    }

    [Fact]
    public async Task GetComponentDetailAsync_WithInvalidComponent_ThrowsMcpException()
    {
        // Arrange
        var indexer = new Mock<IComponentIndexer>();
        indexer.Setup(x => x.GetComponentAsync("Unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComponentInfo?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(async () =>
            await ComponentDetailTools.GetComponentDetailAsync(
                indexer.Object, NullLogger, "Unknown", false, true, CancellationToken.None));

        Assert.Contains("not found", ex.Message);
        Assert.Contains("list_components", ex.Message);
    }

    [Fact]
    public async Task GetComponentDetailAsync_WithExamples_IncludesExamples()
    {
        // Arrange
        var indexer = CreateMockIndexer();

        // Act
        var result = await ComponentDetailTools.GetComponentDetailAsync(
            indexer, NullLogger, "BitButton", false, true, CancellationToken.None);

        // Assert
        Assert.Contains("Examples", result);
        Assert.Contains("Basic", result);
    }

    [Fact]
    public async Task GetComponentDetailAsync_WithNullOptionalParameters_UsesDefaults()
    {
        // Arrange
        var indexer = CreateMockIndexer();

        // Act - simulating what happens when MCP client doesn't send optional parameters
        var result = await ComponentDetailTools.GetComponentDetailAsync(
            indexer, NullLogger, "BitButton", null, null, CancellationToken.None);

        // Assert - default is includeExamples=true, so examples should be included
        Assert.Contains("BitButton", result);
        Assert.Contains("Examples", result);
    }

    [Fact]
    public async Task GetComponentParametersAsync_ReturnsParameters()
    {
        // Arrange
        var indexer = CreateMockIndexer();

        // Act
        var result = await ComponentDetailTools.GetComponentParametersAsync(
            indexer, NullLogger, "BitButton", null, CancellationToken.None);

        // Assert
        Assert.Contains("Color", result);
        Assert.Contains("Variant", result);
    }

    [Fact]
    public async Task GetComponentParametersAsync_WithBoolParameter_ShowsUsageHint()
    {
        // Arrange
        var indexer = CreateMockIndexerWithBoolParam();

        // Act
        var result = await ComponentDetailTools.GetComponentParametersAsync(
            indexer, NullLogger, "BitStack", null, CancellationToken.None);

        // Assert - Bool parameters should show usage hint with true/false
        Assert.Contains("Row", result);
        // Should indicate bool usage syntax: Row="true" or Row="false"
        Assert.Contains("\"true\"", result);
    }

    [Fact]
    public async Task GetComponentParametersAsync_WithEnumParameter_ShowsUsageHint()
    {
        // Arrange
        var indexer = CreateMockIndexerWithEnumParam();

        // Act
        var result = await ComponentDetailTools.GetComponentParametersAsync(
            indexer, NullLogger, "BitStack", null, CancellationToken.None);

        // Assert - Enum parameters should show usage hint with enum type prefix
        Assert.Contains("AlignItems", result);
        // Should indicate enum usage syntax: AlignItems="AlignItems.Center"
        Assert.Contains("AlignItems.", result);
    }

    private static IComponentIndexer CreateMockIndexerWithBoolParam()
    {
        var indexer = new Mock<IComponentIndexer>();

        var component = new ComponentInfo(
            Name: "BitStack",
            Namespace: "Bit.BlazorUI",
            Summary: "A component for stacking items",
            Description: "Stack children vertically or horizontally.",
            Category: "Layouts",
            BaseType: "BitComponentBase",
            Parameters: [
                new ComponentParameter("Row", "bool", "If true, items are stacked horizontally", "false", false, false, "Behavior")
            ],
            Events: [],
            Methods: [],
            Examples: [],
            RelatedComponents: [],
            DocumentationUrl: null,
            SourceUrl: null
        );

        indexer.Setup(x => x.GetComponentAsync("BitStack", It.IsAny<CancellationToken>()))
            .ReturnsAsync(component);

        return indexer.Object;
    }

    private static IComponentIndexer CreateMockIndexerWithEnumParam()
    {
        var indexer = new Mock<IComponentIndexer>();

        var component = new ComponentInfo(
            Name: "BitStack",
            Namespace: "Bit.BlazorUI",
            Summary: "A component for stacking items",
            Description: "Stack children vertically or horizontally.",
            Category: "Layouts",
            BaseType: "BitComponentBase",
            Parameters: [
                new ComponentParameter("AlignItems", "AlignItems", "Defines the alignment of items", "AlignItems.Stretch", false, false, "Behavior")
            ],
            Events: [],
            Methods: [],
            Examples: [],
            RelatedComponents: [],
            DocumentationUrl: null,
            SourceUrl: null
        );

        indexer.Setup(x => x.GetComponentAsync("BitStack", It.IsAny<CancellationToken>()))
            .ReturnsAsync(component);

        return indexer.Object;
    }

    private static IComponentIndexer CreateMockIndexer()
    {
        var indexer = new Mock<IComponentIndexer>();

        var component = new ComponentInfo(
            Name: "BitButton",
            Namespace: "Bit.BlazorUI",
            Summary: "A button component",
            Description: "Use buttons for primary user actions.",
            Category: "Buttons",
            BaseType: "BitComponentBase",
            Parameters: [
                new ComponentParameter("Color", "BitColor", "The button color", "null", false, false, "Appearance"),
                new ComponentParameter("Variant", "BitVariant", "The button variant", "BitVariant.Fill", false, false, "Appearance"),
                new ComponentParameter("IsEnabled", "bool", "Whether the button is enabled", "true", false, false, "Behavior")
            ],
            Events: [
                new ComponentEvent("OnClick", "MouseEventArgs", "Callback when clicked")
            ],
            Methods: [
                new ComponentMethod("FocusAsync", "Task", "Focuses the button", [], true)
            ],
            Examples: [
                new ComponentExample("Basic", "Basic button usage", "<BitButton>Click</BitButton>", null, "BasicExample.razor", [])
            ],
            RelatedComponents: ["BitButton", "BitActionButton"],
            DocumentationUrl: "https://blazorui.bitplatform.dev/components/button",
            SourceUrl: "https://github.com/bitfoundation/bitplatform"
        );

        indexer.Setup(x => x.GetComponentAsync("BitButton", It.IsAny<CancellationToken>()))
            .ReturnsAsync(component);
        indexer.Setup(x => x.GetComponentAsync("Button", It.IsAny<CancellationToken>()))
            .ReturnsAsync(component);

        return indexer.Object;
    }
}
