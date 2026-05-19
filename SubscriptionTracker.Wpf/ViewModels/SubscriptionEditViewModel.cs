using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Enums;
using SubscriptionTracker.Domain.Services;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class SubscriptionEditViewModel : ObservableObject
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private Guid _selectedCategoryId;
    private decimal _amount;
    private string _selectedCurrency = "RUB";
    private BillingCycle _selectedBillingCycle = BillingCycle.Monthly;
    private DateTime _firstPaymentDate = DateTime.Today;
    private DateTime _nextPaymentDate = DateTime.Today;
    private bool _isActive = true;
    private bool _autoRenewal = true;
    private bool _isLowUsage;
    private int _reminderDaysBefore = 3;

    private SubscriptionEditViewModel()
    {
    }

    public Guid? Id { get; private set; }

    public IReadOnlyList<CategoryOptionDto> Categories { get; private init; } = Array.Empty<CategoryOptionDto>();

    public IReadOnlyList<string> Currencies { get; private init; } = CurrencyConverter.GetSupportedCurrencies().ToArray();

    public IReadOnlyList<BillingCycle> BillingCycles { get; } =
    [
        BillingCycle.Monthly,
        BillingCycle.Quarterly,
        BillingCycle.SemiAnnual,
        BillingCycle.Yearly
    ];

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public Guid SelectedCategoryId
    {
        get => _selectedCategoryId;
        set => SetProperty(ref _selectedCategoryId, value);
    }

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    public string SelectedCurrency
    {
        get => _selectedCurrency;
        set => SetProperty(ref _selectedCurrency, value);
    }

    public BillingCycle SelectedBillingCycle
    {
        get => _selectedBillingCycle;
        set => SetProperty(ref _selectedBillingCycle, value);
    }

    public DateTime FirstPaymentDate
    {
        get => _firstPaymentDate;
        set => SetProperty(ref _firstPaymentDate, value);
    }

    public DateTime NextPaymentDate
    {
        get => _nextPaymentDate;
        set => SetProperty(ref _nextPaymentDate, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public bool AutoRenewal
    {
        get => _autoRenewal;
        set => SetProperty(ref _autoRenewal, value);
    }

    public int ReminderDaysBefore
    {
        get => _reminderDaysBefore;
        set => SetProperty(ref _reminderDaysBefore, value);
    }

    public bool IsLowUsage
    {
        get => _isLowUsage;
        set => SetProperty(ref _isLowUsage, value);
    }

    public string WindowTitle => Id.HasValue
        ? LocalizationCatalog.Get("EditSubscriptionTitle")
        : LocalizationCatalog.Get("NewSubscriptionTitle");

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return LocalizationCatalog.Get("ValidationNameRequired");
        }

        if (SelectedCategoryId == Guid.Empty)
        {
            return LocalizationCatalog.Get("ValidationCategoryRequired");
        }

        if (Amount <= 0)
        {
            return LocalizationCatalog.Get("ValidationAmountPositive");
        }

        if (ReminderDaysBefore < 0)
        {
            return LocalizationCatalog.Get("ValidationReminderNonNegative");
        }

        return null;
    }

    public SaveSubscriptionRequest BuildRequest()
    {
        return new SaveSubscriptionRequest
        {
            Id = Id,
            Name = Name,
            Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            CategoryId = SelectedCategoryId,
            Amount = Amount,
            Currency = SelectedCurrency,
            BillingCycle = SelectedBillingCycle,
            FirstPaymentDate = DateOnly.FromDateTime(FirstPaymentDate.Date),
            NextPaymentDate = DateOnly.FromDateTime(NextPaymentDate.Date),
            IsActive = IsActive,
            AutoRenewal = AutoRenewal,
            ReminderDaysBefore = ReminderDaysBefore,
            IsLowUsage = IsLowUsage
        };
    }

    public static async Task<SubscriptionEditViewModel> CreateAsync(
        ICategoryService categoryService,
        SubscriptionListItemDto? currentItem,
        CancellationToken cancellationToken = default)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        var viewModel = new SubscriptionEditViewModel
        {
            Categories = categories,
            SelectedCategoryId = currentItem?.CategoryId ?? categories.FirstOrDefault()?.Id ?? Guid.Empty,
            Id = currentItem?.Id,
            Name = currentItem?.Name ?? string.Empty,
            Description = currentItem?.Description ?? string.Empty,
            Amount = currentItem?.Amount ?? 0m,
            SelectedCurrency = currentItem?.Currency ?? "RUB",
            SelectedBillingCycle = currentItem?.BillingCycle ?? BillingCycle.Monthly,
            FirstPaymentDate = currentItem?.FirstPaymentDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today,
            NextPaymentDate = currentItem?.NextPaymentDate.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today,
            IsActive = currentItem?.IsActive ?? true,
            AutoRenewal = currentItem?.AutoRenewal ?? true,
            IsLowUsage = currentItem?.IsLowUsage ?? false,
            ReminderDaysBefore = currentItem?.ReminderDaysBefore ?? 3
        };

        return viewModel;
    }
}
