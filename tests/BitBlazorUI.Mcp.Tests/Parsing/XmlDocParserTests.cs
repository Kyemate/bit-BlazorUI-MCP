// Copyright (c) 2025 Bit BlazorUI MCP Contributors
// Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using BitBlazorUI.Mcp.Services.Parsing;

namespace BitBlazorUI.Mcp.Tests.Parsing;

public class XmlDocParserTests
{
    private readonly XmlDocParser _parser;

    public XmlDocParserTests()
    {
        var logger = Mock.Of<ILogger<XmlDocParser>>();
        _parser = new XmlDocParser(logger);
    }

    [Fact]
    public void ParseSourceCode_WithValidComponent_ExtractsClassName()
    {
        // Arrange
        var source = """
            namespace Bit.BlazorUI;

            /// <summary>
            /// A button component.
            /// </summary>
            public class BitButton : BitComponentBase
            {
                /// <summary>
                /// The color of the button.
                /// </summary>
                [Parameter]
                public BitColor? Color { get; set; } = null;
            }
            """;

        // Act
        var result = _parser.ParseSourceCode(source, "BitButton.razor.cs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BitButton", result.ClassName);
        Assert.Equal("Bit.BlazorUI", result.Namespace);
        Assert.Equal("BitComponentBase", result.BaseType);
    }

    [Fact]
    public void ParseSourceCode_WithXmlDocumentation_ExtractsSummary()
    {
        // Arrange
        var source = """
            namespace Bit.BlazorUI;

            /// <summary>
            /// A button component.
            /// </summary>
            /// <remarks>
            /// Use this component for primary user actions.
            /// </remarks>
            public class BitButton : BitComponentBase
            {
            }
            """;

        // Act
        var result = _parser.ParseSourceCode(source, "BitButton.razor.cs");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("button", result.Summary);
        Assert.Contains("primary user actions", result.Remarks);
    }

    [Fact]
    public void ParseSourceCode_WithParameters_ExtractsParameters()
    {
        // Arrange
        var source = """
            namespace Bit.BlazorUI;

            public class BitButton : BitComponentBase
            {
                /// <summary>
                /// The color of the button.
                /// </summary>
                [Parameter]
                public BitColor? Color { get; set; } = null;

                /// <summary>
                /// The size of the button.
                /// </summary>
                [Parameter]
                public BitSize? Size { get; set; }

                /// <summary>
                /// Gets or sets whether the button is enabled.
                /// </summary>
                [Parameter]
                [EditorRequired]
                public bool IsEnabled { get; set; } = true;
            }
            """;

        // Act
        var result = _parser.ParseSourceCode(source, "BitButton.razor.cs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Parameters.Count);

        var colorParam = result.Parameters.First(p => p.Name == "Color");
        Assert.Equal("BitColor?", colorParam.Type);
        Assert.Contains("color of the button", colorParam.Description);
        Assert.Equal("null", colorParam.DefaultValue);

        var enabledParam = result.Parameters.First(p => p.Name == "IsEnabled");
        Assert.True(enabledParam.IsRequired);
    }

    [Fact]
    public void ParseSourceCode_WithEvents_ExtractsEventCallbacks()
    {
        // Arrange
        var source = """
            namespace Bit.BlazorUI;

            public class BitButton : BitComponentBase
            {
                /// <summary>
                /// Callback when the button is clicked.
                /// </summary>
                [Parameter]
                public EventCallback<MouseEventArgs> OnClick { get; set; }

                /// <summary>
                /// Callback when the mouse enters the button.
                /// </summary>
                [Parameter]
                public EventCallback OnMouseEnter { get; set; }
            }
            """;

        // Act
        var result = _parser.ParseSourceCode(source, "BitButton.razor.cs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Events.Count);

        var onClick = result.Events.First(e => e.Name == "OnClick");
        Assert.Equal("MouseEventArgs", onClick.EventArgsType);

        var onMouseEnter = result.Events.First(e => e.Name == "OnMouseEnter");
        Assert.Null(onMouseEnter.EventArgsType);
    }

    [Fact]
    public void ParseSourceCode_WithPublicMethods_ExtractsMethods()
    {
        // Arrange
        var source = """
            namespace Bit.BlazorUI;

            public class BitButton : BitComponentBase
            {
                /// <summary>
                /// Focuses the button element.
                /// </summary>
                public async Task FocusAsync()
                {
                }

                /// <summary>
                /// Clicks the button programmatically.
                /// </summary>
                public void Click()
                {
                }

                // This should not be included
                private void InternalMethod()
                {
                }
            }
            """;

        // Act
        var result = _parser.ParseSourceCode(source, "BitButton.razor.cs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Methods.Count);

        var focusAsync = result.Methods.First(m => m.Name == "FocusAsync");
        Assert.True(focusAsync.IsAsync);
        Assert.Equal("Task", focusAsync.ReturnType);

        var click = result.Methods.First(m => m.Name == "Click");
        Assert.False(click.IsAsync);
        Assert.Equal("void", click.ReturnType);
    }

    [Fact]
    public void ParseSourceCode_WithNoPublicClass_ReturnsNull()
    {
        // Arrange
        var source = """
            namespace Bit.BlazorUI;

            internal class InternalComponent
            {
            }
            """;

        // Act
        var result = _parser.ParseSourceCode(source, "Internal.cs");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseSourceCode_WithFileScopedNamespace_ExtractsNamespace()
    {
        // Arrange
        var source = """
            namespace Bit.BlazorUI;

            public class BitButton : BitComponentBase
            {
            }
            """;

        // Act
        var result = _parser.ParseSourceCode(source, "BitButton.razor.cs");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Bit.BlazorUI", result.Namespace);
    }

    [Fact]
    public void ParseSourceCode_WithCascadingParameter_IdentifiesAsCascading()
    {
        // Arrange
        var source = """
            namespace Bit.BlazorUI;

            public class BitNavLink : BitComponentBase
            {
                [CascadingParameter]
                public BitNav? Nav { get; set; }
            }
            """;

        // Act
        var result = _parser.ParseSourceCode(source, "BitNavLink.razor.cs");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Parameters);
        Assert.True(result.Parameters[0].IsCascading);
    }
}
