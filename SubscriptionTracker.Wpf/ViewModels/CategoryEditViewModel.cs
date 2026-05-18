using System.Text.RegularExpressions;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class CategoryEditViewModel : ViewModelBase
{
    private static readonly Regex ColorHexPattern = new("^#([0-9A-Fa-f]{6})$", RegexOptions.Compiled);
    private string _name = string.Empty;
    private string _colorHex = string.Empty;

    public CategoryEditViewModel(CategoryListItemDto? currentItem)
    {
        Id = currentItem?.Id;
        _name = currentItem?.Name ?? string.Empty;
        _colorHex = currentItem?.ColorHex ?? string.Empty;
    }

    public Guid? Id { get; }

    public bool IsEditMode => Id.HasValue;

    public string WindowTitle => LocalizationCatalog.Get(IsEditMode ? "CategoryEditDialogTitle" : "CategoryCreateDialogTitle");

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string ColorHex
    {
        get => _colorHex;
        set => SetProperty(ref _colorHex, value);
    }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return LocalizationCatalog.Get("CategoryNameRequired");
        }

        if (!string.IsNullOrWhiteSpace(ColorHex) && !ColorHexPattern.IsMatch(ColorHex.Trim()))
        {
            return LocalizationCatalog.Get("CategoryColorInvalid");
        }

        return null;
    }

    public SaveCategoryRequest BuildRequest()
    {
        return new SaveCategoryRequest
        {
            Id = Id,
            Name = Name.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(ColorHex) ? null : ColorHex.Trim().ToUpperInvariant()
        };
    }
}
