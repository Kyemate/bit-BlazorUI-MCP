// Copyright (c) 2025 Bit BlazorUI MCP Contributors
// Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using BitBlazorUI.Mcp.Services.Parsing;

namespace BitBlazorUI.Mcp.Tests.Parsing;

public class CategoryMapperTests
{
    private readonly CategoryMapper _mapper;

    public CategoryMapperTests()
    {
        var logger = Mock.Of<ILogger<CategoryMapper>>();
        _mapper = new CategoryMapper(logger);
    }

    [Fact]
    public async Task InitializeAsync_SetsUpCategories()
    {
        // Act
        await _mapper.InitializeAsync("/repo", CancellationToken.None);

        // Assert
        var categories = _mapper.GetCategories();
        Assert.NotEmpty(categories);
    }

    [Theory]
    [InlineData("BitButton", "Buttons")]
    [InlineData("BitTextField", "Inputs")]
    [InlineData("BitDropdown", "Inputs")]
    [InlineData("BitNav", "Navs")]
    [InlineData("BitDataGrid", "Extras")]
    [InlineData("BitMessageBar", "Notifications")]
    [InlineData("BitCard", "Surfaces")]
    public async Task GetCategoryName_ReturnsCorrectCategory(string componentName, string expectedCategory)
    {
        // Arrange
        await _mapper.InitializeAsync("/repo", CancellationToken.None);

        // Act
        var category = _mapper.GetCategoryName(componentName);

        // Assert
        Assert.Equal(expectedCategory, category);
    }

    [Fact]
    public async Task GetCategoryForComponent_ReturnsFullCategoryInfo()
    {
        // Arrange
        await _mapper.InitializeAsync("/repo", CancellationToken.None);

        // Act
        var category = _mapper.GetCategoryForComponent("BitButton");

        // Assert
        Assert.NotNull(category);
        Assert.Equal("Buttons", category.Name);
        Assert.NotNull(category.Description);
        Assert.NotEmpty(category.Description);
        Assert.Contains("BitButton", category.ComponentNames);
    }

    [Fact]
    public async Task GetComponentsInCategory_ReturnsComponentsInCategory()
    {
        // Arrange
        await _mapper.InitializeAsync("/repo", CancellationToken.None);

        // Act
        var components = _mapper.GetComponentsInCategory("Buttons");

        // Assert
        Assert.NotEmpty(components);
        Assert.Contains("BitButton", components);
    }

    [Theory]
    [InlineData("BitNewButton", "Buttons")]
    [InlineData("BitCustomTextField", "Inputs")]
    [InlineData("BitSpecialChart", "Extras")]
    [InlineData("BitCustomDialog", "Surfaces")]
    public async Task InferCategoryFromName_InfersCorrectCategory(string componentName, string expectedCategory)
    {
        // Arrange
        await _mapper.InitializeAsync("/repo", CancellationToken.None);

        // Act
        var category = _mapper.InferCategoryFromName(componentName);

        // Assert
        Assert.Equal(expectedCategory, category);
    }

    [Fact]
    public async Task GetCategoryName_UnknownComponent_ReturnsNull()
    {
        // Arrange
        await _mapper.InitializeAsync("/repo", CancellationToken.None);

        // Act
        var category = _mapper.GetCategoryName("UnknownComponent");

        // Assert
        Assert.Null(category);
    }
}
