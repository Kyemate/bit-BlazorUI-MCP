// Copyright (c) 2026 Bit BlazorUI MCP Contributors
// Licensed under the GNU General Public License v2.0. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using BitBlazorUI.Mcp.Models;

namespace BitBlazorUI.Mcp.Services.Parsing;

/// <summary>
/// Maps components to their categories based on Bit BlazorUI's component categories.
/// </summary>
public sealed class CategoryMapper
{
    private readonly ILogger<CategoryMapper> _logger;
    private readonly Dictionary<string, ComponentCategory> _categoryMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ComponentCategory> _categories = [];
    private bool _isInitialized;

    public CategoryMapper(ILogger<CategoryMapper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the category mapper from the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the Bit BlazorUI repository.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the initialization operation.</returns>
    public Task InitializeAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryPath);

        if (_isInitialized)
            return Task.CompletedTask;

        // Initialize with Bit BlazorUI's known categories
        // These are derived from the component structure in Bit BlazorUI repository
        InitializeKnownCategories();

        _isInitialized = true;
        _logger.LogInformation("Category mapper initialized with {Count} categories", _categories.Count);

        return Task.CompletedTask;
    }

    private void InitializeKnownCategories()
    {
        // Categories from Bit BlazorUI's component structure
        AddCategory("Buttons", "Buttons", "Interactive button components",
            "BitButton", "BitButtonGroup", "BitMenuButton", "BitToggleButton", "BitActionButton");

        AddCategory("Inputs", "Inputs", "Form input and control components",
            "BitTextField", "BitNumberField", "BitSearchBox", "BitCheckbox", "BitChoiceGroup",
            "BitDropdown", "BitToggle", "BitSlider", "BitRating", "BitDatePicker",
            "BitTimePicker", "BitDateRangePicker", "BitColorPicker", "BitFileUpload",
            "BitOtpInput", "BitCircularTimePicker");

        AddCategory("Navs", "Navs", "Navigation components",
            "BitNav", "BitNavBar", "BitBreadcrumb", "BitPagination", "BitPivot");

        AddCategory("Surfaces", "Surfaces", "Surface and container components",
            "BitAccordion", "BitCard", "BitDialog", "BitModal", "BitPanel",
            "BitTooltip", "BitCallout", "BitPopover", "BitScrollablePane");

        AddCategory("Notifications", "Notifications", "Notification and feedback components",
            "BitMessageBar", "BitSnackBar", "BitBadge", "BitPersona", "BitTag");

        AddCategory("Lists", "Lists", "List and data display components",
            "BitBasicList", "BitTimeline", "BitCarousel", "BitSwiper");

        AddCategory("Layouts", "Layouts", "Layout and structure components",
            "BitGrid", "BitStack", "BitSpacer", "BitSeparator", "BitHeader",
            "BitFooter", "BitLayout");

        AddCategory("Progress", "Progress", "Progress and loading indicators",
            "BitProgress", "BitLoading", "BitShimmer");

        AddCategory("Utilities", "Utilities", "Utility components and helpers",
            "BitIcon", "BitImage", "BitLink", "BitText", "BitLabel",
            "BitElement", "BitOverlay", "BitStickyHeader");

        AddCategory("Extras", "Extras", "Additional components and extensions",
            "BitChart", "BitDataGrid", "BitPdfReader");
    }

    private void AddCategory(string name, string title, string description, params string[] components)
    {
        var category = new ComponentCategory(
            Name: name,
            Title: title,
            Description: description,
            ComponentNames: components.ToList()
        );

        _categories.Add(category);

        foreach (var component in components)
        {
            _categoryMap[component] = category;
        }
    }

    /// <summary>
    /// Gets all categories.
    /// </summary>
    public IReadOnlyList<ComponentCategory> GetCategories()
    {
        return _categories.AsReadOnly();
    }

    /// <summary>
    /// Gets the category for a component.
    /// </summary>
    /// <param name="componentName">The component name.</param>
    /// <returns>The category, or null if not found.</returns>
    public ComponentCategory? GetCategoryForComponent(string componentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);
        return _categoryMap.GetValueOrDefault(componentName);
    }

    /// <summary>
    /// Gets the category name for a component.
    /// </summary>
    /// <param name="componentName">The component name.</param>
    /// <returns>The category name, or null if not found.</returns>
    public string? GetCategoryName(string componentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);
        return _categoryMap.TryGetValue(componentName, out var category) ? category.Name : null;
    }

    /// <summary>
    /// Gets components in a specific category.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>A list of component names in the category.</returns>
    public IReadOnlyList<string> GetComponentsInCategory(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        var category = _categories.FirstOrDefault(c =>
            c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) ||
            (c.Title?.Equals(categoryName, StringComparison.OrdinalIgnoreCase) == true));

        return category?.ComponentNames ?? [];
    }

    /// <summary>
    /// Tries to determine category from component name patterns.
    /// </summary>
    /// <param name="componentName">The component name to analyze.</param>
    /// <returns>The inferred category name.</returns>
    public string? InferCategoryFromName(string componentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentName);

        // Remove "Bit" prefix for pattern matching
        var baseName = componentName.StartsWith("Bit") ? componentName[3..] : componentName;

        // Pattern-based category inference
        return baseName.ToLowerInvariant() switch
        {
            var n when n.Contains("button") => "Buttons",
            var n when n.Contains("textfield") || n.Contains("numberfield") || n.Contains("searchbox") ||
                       n.Contains("checkbox") || n.Contains("choicegroup") || n.Contains("dropdown") ||
                       n.Contains("toggle") || n.Contains("slider") || n.Contains("rating") ||
                       n.Contains("picker") || n.Contains("fileupload") || n.Contains("otpinput") => "Inputs",
            var n when n.Contains("nav") || n.Contains("navbar") || n.Contains("breadcrumb") ||
                       n.Contains("pagination") || n.Contains("pivot") => "Navs",
            var n when n.Contains("accordion") || n.Contains("card") || n.Contains("dialog") ||
                       n.Contains("modal") || n.Contains("panel") || n.Contains("tooltip") ||
                       n.Contains("callout") || n.Contains("popover") || n.Contains("scrollablepane") => "Surfaces",
            var n when n.Contains("messagebar") || n.Contains("snackbar") || n.Contains("badge") ||
                       n.Contains("persona") || n.Contains("tag") => "Notifications",
            var n when n.Contains("basiclist") || n.Contains("timeline") || n.Contains("carousel") ||
                       n.Contains("swiper") => "Lists",
            var n when n.Contains("grid") || n.Contains("stack") || n.Contains("spacer") ||
                       n.Contains("separator") || n.Contains("header") || n.Contains("footer") ||
                       n.Contains("layout") => "Layouts",
            var n when n.Contains("progress") || n.Contains("loading") || n.Contains("shimmer") => "Progress",
            var n when n.Contains("chart") || n.Contains("datagrid") || n.Contains("pdfreader") => "Extras",
            var n when n.Contains("icon") || n.Contains("image") || n.Contains("link") ||
                       n.Contains("text") || n.Contains("label") || n.Contains("element") ||
                       n.Contains("overlay") || n.Contains("stickyheader") => "Utilities",
            _ => "Utilities"
        };
    }
}
