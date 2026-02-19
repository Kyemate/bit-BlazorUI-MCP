// Copyright (c) 2025 Bit BlazorUI MCP Contributors
// Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BitBlazorUI.Mcp.Models;
using BitBlazorUI.Mcp.Services;
using BitBlazorUI.Mcp.Tools;

namespace BitBlazorUI.Mcp.Tests.Tools;

public class ComponentExampleToolsTests
{
    private static readonly ILogger<ComponentExampleTools> NullLogger =
        NullLoggerFactory.Instance.CreateLogger<ComponentExampleTools>();

    #region GetComponentExamplesAsync Tests

    [Fact]
    public async Task GetComponentExamplesAsync_WithValidComponent_ReturnsExamples()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act
        var result = await ComponentExampleTools.GetComponentExamplesAsync(
            indexer, NullLogger, "BitButton", 5, null, CancellationToken.None);

        // Assert
        Assert.Contains("BitButton", result);
        Assert.Contains("Examples", result);
        Assert.Contains("Basic Button", result);
    }

    [Fact]
    public async Task GetComponentExamplesAsync_WithNullMaxExamples_UsesDefaultValue()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act - simulating what happens when MCP client doesn't send maxExamples
        var result = await ComponentExampleTools.GetComponentExamplesAsync(
            indexer, NullLogger, "BitButton", null, null, CancellationToken.None);

        // Assert
        Assert.Contains("BitButton", result);
        Assert.Contains("Examples", result);
    }

    [Fact]
    public async Task GetComponentExamplesAsync_WithFilter_ReturnsFilteredExamples()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act
        var result = await ComponentExampleTools.GetComponentExamplesAsync(
            indexer, NullLogger, "BitButton", 5, "icon", CancellationToken.None);

        // Assert
        Assert.Contains("Icon Button", result);
    }

    [Fact]
    public async Task GetComponentExamplesAsync_WithEmptyComponentName_ThrowsMcpException()
    {
        // Arrange
        var indexer = new Mock<IComponentIndexer>();

        // Act & Assert
        await Assert.ThrowsAsync<ModelContextProtocol.McpException>(async () =>
            await ComponentExampleTools.GetComponentExamplesAsync(
                indexer.Object, NullLogger, "", 5, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetComponentExamplesAsync_WithNullComponentName_ThrowsMcpException()
    {
        // Arrange
        var indexer = new Mock<IComponentIndexer>();

        // Act & Assert
        await Assert.ThrowsAsync<ModelContextProtocol.McpException>(async () =>
            await ComponentExampleTools.GetComponentExamplesAsync(
                indexer.Object, NullLogger, null!, 5, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetComponentExamplesAsync_WithInvalidComponent_ThrowsMcpException()
    {
        // Arrange
        var indexer = new Mock<IComponentIndexer>();
        indexer.Setup(x => x.GetComponentAsync("Unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ComponentInfo?)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(async () =>
            await ComponentExampleTools.GetComponentExamplesAsync(
                indexer.Object, NullLogger, "Unknown", 5, null, CancellationToken.None));

        Assert.Contains("not found", ex.Message);
        Assert.Contains("list_components", ex.Message);
    }

    [Fact]
    public async Task GetComponentExamplesAsync_WithMaxExamplesOutOfRange_ThrowsMcpException()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act & Assert - maxExamples = 0 is out of range (min is 1)
        await Assert.ThrowsAsync<ModelContextProtocol.McpException>(async () =>
            await ComponentExampleTools.GetComponentExamplesAsync(
                indexer, NullLogger, "BitButton", 0, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetComponentExamplesAsync_WithMaxExamplesTooHigh_ThrowsMcpException()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act & Assert - maxExamples = 100 is out of range (max is 20)
        await Assert.ThrowsAsync<ModelContextProtocol.McpException>(async () =>
            await ComponentExampleTools.GetComponentExamplesAsync(
                indexer, NullLogger, "BitButton", 100, null, CancellationToken.None));
    }

    #endregion

    #region GetExampleByNameAsync Tests

    [Fact]
    public async Task GetExampleByNameAsync_WithValidExample_ReturnsExample()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act
        var result = await ComponentExampleTools.GetExampleByNameAsync(
            indexer, NullLogger, "BitButton", "Basic Button", CancellationToken.None);

        // Assert
        Assert.Contains("BitButton", result);
        Assert.Contains("Basic Button", result);
        Assert.Contains("<BitButton>Click Me</BitButton>", result);
    }

    [Fact]
    public async Task GetExampleByNameAsync_WithFuzzyMatch_ReturnsExample()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act
        var result = await ComponentExampleTools.GetExampleByNameAsync(
            indexer, NullLogger, "BitButton", "Basic", CancellationToken.None);

        // Assert
        Assert.Contains("Basic Button", result);
    }

    [Fact]
    public async Task GetExampleByNameAsync_WithFeatureMatch_ReturnsExample()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act
        var result = await ComponentExampleTools.GetExampleByNameAsync(
            indexer, NullLogger, "BitButton", "fluent", CancellationToken.None);

        // Assert
        Assert.Contains("Icon Button", result);
    }

    [Fact]
    public async Task GetExampleByNameAsync_WithInvalidExample_ThrowsMcpException()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(async () =>
            await ComponentExampleTools.GetExampleByNameAsync(
                indexer, NullLogger, "BitButton", "NonExistent", CancellationToken.None));

        Assert.Contains("not found", ex.Message);
    }

    #endregion

    #region ListComponentExamplesAsync Tests

    [Fact]
    public async Task ListComponentExamplesAsync_WithValidComponent_ReturnsExampleList()
    {
        // Arrange
        var indexer = CreateMockIndexerWithExamples();

        // Act
        var result = await ComponentExampleTools.ListComponentExamplesAsync(
            indexer, NullLogger, "BitButton", CancellationToken.None);

        // Assert
        Assert.Contains("BitButton", result);
        Assert.Contains("Basic Button", result);
        Assert.Contains("Icon Button", result);
        Assert.Contains("3 example(s)", result);
    }

    [Fact]
    public async Task ListComponentExamplesAsync_WithNoExamples_ReturnsNoExamplesMessage()
    {
        // Arrange
        var indexer = new Mock<IComponentIndexer>();
        var component = new ComponentInfo(
            Name: "BitEmpty",
            Namespace: "Bit.BlazorUI",
            Summary: "An empty component",
            Description: null,
            Category: "Test",
            BaseType: null,
            Parameters: [],
            Events: [],
            Methods: [],
            Examples: [],
            RelatedComponents: [],
            DocumentationUrl: null,
            SourceUrl: null
        );
        indexer.Setup(x => x.GetComponentAsync("BitEmpty", It.IsAny<CancellationToken>()))
            .ReturnsAsync(component);

        // Act
        var result = await ComponentExampleTools.ListComponentExamplesAsync(
            indexer.Object, NullLogger, "BitEmpty", CancellationToken.None);

        // Assert
        Assert.Contains("No examples available", result);
    }

    #endregion

    #region Helper Methods

    private static IComponentIndexer CreateMockIndexerWithExamples()
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
                new ComponentParameter("Variant", "BitVariant", "The button variant", "BitVariant.Fill", false, false, "Appearance")
            ],
            Events: [
                new ComponentEvent("OnClick", "MouseEventArgs", "Callback when clicked")
            ],
            Methods: [],
            Examples: [
                new ComponentExample(
                    "Basic Button",
                    "A basic button example",
                    "<BitButton>Click Me</BitButton>",
                    null,
                    "BasicButtonExample.razor",
                    ["basic", "simple"]
                ),
                new ComponentExample(
                    "Icon Button",
                    "A button with an icon",
                    "<BitButton IconName=\"@BitIconName.Add\">Add</BitButton>",
                    null,
                    "IconButtonExample.razor",
                    ["icon", "fluent"]
                ),
                new ComponentExample(
                    "Disabled Button",
                    "A disabled button",
                    "<BitButton IsEnabled=\"false\">Disabled</BitButton>",
                    null,
                    "DisabledButtonExample.razor",
                    ["disabled", "state"]
                )
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

    #endregion
}
