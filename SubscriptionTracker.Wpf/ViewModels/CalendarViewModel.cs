using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class CalendarViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILocalizationService _localizationService;
    private readonly List<UpcomingPaymentDto> _allPayments = [];
    private IReadOnlyList<CalendarMonthGroup> _months = Array.Empty<CalendarMonthGroup>();
    private IReadOnlyList<OptionItem<string>> _categoryFilters = [];
    private OptionItem<string>? _selectedCategoryFilter;
    private string _searchText = string.Empty;
    private DateTime? _fromDate;
    private DateTime? _toDate;

    public CalendarViewModel(IServiceScopeFactory scopeFactory, ILocalizationService localizationService)
    {
        _scopeFactory = scopeFactory;
        _localizationService = localizationService;
        _localizationService.LanguageChanged += (_, _) => RebuildCategoryFilters();
    }

    public IReadOnlyList<CalendarMonthGroup> Months
    {
        get => _months;
        private set => SetProperty(ref _months, value);
    }

    public IReadOnlyList<OptionItem<string>> CategoryFilters
    {
        get => _categoryFilters;
        private set => SetProperty(ref _categoryFilters, value);
    }

    public OptionItem<string>? SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set
        {
            if (SetProperty(ref _selectedCategoryFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
            {
                ApplyFilters();
            }
        }
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value))
            {
                ApplyFilters();
            }
        }
    }

    public int VisiblePaymentsCount => Months.Sum(static month => month.Payments.Count);

    public int VisibleMonthsCount => Months.Count;

    public string VisibleScheduledTotalLabel
    {
        get
        {
            var firstCurrency = Months.SelectMany(static month => month.Payments).FirstOrDefault()?.BaseCurrency
                ?? _allPayments.FirstOrDefault()?.BaseCurrency
                ?? "RUB";

            var total = Months.Sum(static month => month.TotalAmountInBaseCurrency);
            return $"{total:N2} {firstCurrency}";
        }
    }

    public override async Task RefreshAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        IsBusy = true;
        try
        {
            var months = await subscriptionService.GetCalendarAsync();
            _allPayments.Clear();
            _allPayments.AddRange(months.SelectMany(static month => month.Payments));
            RebuildCategoryFilters();
            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RebuildCategoryFilters()
    {
        var selectedCategoryValue = SelectedCategoryFilter?.Value;
        var categoryOptions = _allPayments
            .Select(static item => item.CategoryName)
            .Where(static category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static category => category)
            .Select(category => new OptionItem<string> { Value = category, Label = category })
            .ToList();

        categoryOptions.Insert(0, new OptionItem<string>
        {
            Value = string.Empty,
            Label = LocalizationCatalog.Get("FilterAllCategories")
        });

        CategoryFilters = categoryOptions;
        SelectedCategoryFilter = CategoryFilters.FirstOrDefault(item => string.Equals(item.Value, selectedCategoryValue, StringComparison.OrdinalIgnoreCase))
            ?? CategoryFilters.FirstOrDefault();
    }

    private void ApplyFilters()
    {
        var selectedCategory = SelectedCategoryFilter?.Value;
        var filteredItems = _allPayments.Where(item =>
        {
            var matchesCategory = string.IsNullOrWhiteSpace(selectedCategory)
                || string.Equals(item.CategoryName, selectedCategory, StringComparison.OrdinalIgnoreCase);

            var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
                || item.SubscriptionName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || item.CategoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || item.AmountLabel.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

            var matchesFromDate = FromDate is null || item.PaymentDate >= DateOnly.FromDateTime(FromDate.Value);
            var matchesToDate = ToDate is null || item.PaymentDate <= DateOnly.FromDateTime(ToDate.Value);

            return matchesCategory && matchesSearch && matchesFromDate && matchesToDate;
        });

        Months = filteredItems
            .OrderBy(static item => item.PaymentDate)
            .GroupBy(item => new DateOnly(item.PaymentDate.Year, item.PaymentDate.Month, 1))
            .Select(group => new CalendarMonthGroup(
                group.Key.ToString("MMMM yyyy"),
                group.ToArray()))
            .ToArray();

        RaisePropertyChanged(nameof(VisiblePaymentsCount));
        RaisePropertyChanged(nameof(VisibleMonthsCount));
        RaisePropertyChanged(nameof(VisibleScheduledTotalLabel));
    }

    public sealed class CalendarMonthGroup(string title, IReadOnlyList<UpcomingPaymentDto> payments)
    {
        public string Title { get; } = title;

        public IReadOnlyList<UpcomingPaymentDto> Payments { get; } = payments;

        public decimal TotalAmountInBaseCurrency => Payments.Sum(static payment => payment.AmountInBaseCurrency);

        public string TotalAmountLabel
        {
            get
            {
                var baseCurrency = Payments.FirstOrDefault()?.BaseCurrency ?? "RUB";
                return $"{TotalAmountInBaseCurrency:N2} {baseCurrency}";
            }
        }
    }
}
