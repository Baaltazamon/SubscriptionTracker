using System.Windows;
using System.Windows.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SubscriptionTracker.Application.DTO;
using SubscriptionTracker.Application.Interfaces;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Domain.Services;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf.ViewModels;

public sealed class AnalyticsViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppSettingsService _appSettingsService;
    private DashboardSummaryDto _summary = new();
    private decimal _potentialSavings;
    private ISeries[] _forecastSeries = [];
    private Axis[] _forecastXAxes = [];
    private Axis[] _forecastYAxes = [];
    private ISeries[] _categorySeries = [];
    private ISeries[] _upcomingSeries = [];
    private Axis[] _upcomingXAxes = [];
    private Axis[] _upcomingYAxes = [];
    private IReadOnlyList<AnalyticsTopSubscriptionItem> _topSubscriptions = [];
    private IReadOnlyList<AnalyticsUpcomingChargeItem> _upcomingCharges = [];
    private string _upcoming30DaysTotalLabel = "0";
    private int _upcoming30DaysCount;

    public AnalyticsViewModel(
        IServiceScopeFactory scopeFactory,
        IAppSettingsService appSettingsService,
        IThemeService themeService)
    {
        _scopeFactory = scopeFactory;
        _appSettingsService = appSettingsService;
        themeService.ThemeChanged += (_, _) => RebuildCharts();
    }

    public DashboardSummaryDto Summary
    {
        get => _summary;
        private set
        {
            if (SetProperty(ref _summary, value))
            {
                RaisePropertyChanged(nameof(PotentialSavingsLabel));
            }
        }
    }

    public string PotentialSavingsLabel => $"{_potentialSavings:N2} {Summary.BaseCurrency}";

    public ISeries[] ForecastSeries
    {
        get => _forecastSeries;
        private set => SetProperty(ref _forecastSeries, value);
    }

    public Axis[] ForecastXAxes
    {
        get => _forecastXAxes;
        private set => SetProperty(ref _forecastXAxes, value);
    }

    public Axis[] ForecastYAxes
    {
        get => _forecastYAxes;
        private set => SetProperty(ref _forecastYAxes, value);
    }

    public ISeries[] CategorySeries
    {
        get => _categorySeries;
        private set => SetProperty(ref _categorySeries, value);
    }

    public ISeries[] UpcomingSeries
    {
        get => _upcomingSeries;
        private set => SetProperty(ref _upcomingSeries, value);
    }

    public Axis[] UpcomingXAxes
    {
        get => _upcomingXAxes;
        private set => SetProperty(ref _upcomingXAxes, value);
    }

    public Axis[] UpcomingYAxes
    {
        get => _upcomingYAxes;
        private set => SetProperty(ref _upcomingYAxes, value);
    }

    public IReadOnlyList<AnalyticsTopSubscriptionItem> TopSubscriptions
    {
        get => _topSubscriptions;
        private set => SetProperty(ref _topSubscriptions, value);
    }

    public IReadOnlyList<AnalyticsUpcomingChargeItem> UpcomingCharges
    {
        get => _upcomingCharges;
        private set => SetProperty(ref _upcomingCharges, value);
    }

    public string Upcoming30DaysTotalLabel
    {
        get => _upcoming30DaysTotalLabel;
        private set => SetProperty(ref _upcoming30DaysTotalLabel, value);
    }

    public int Upcoming30DaysCount
    {
        get => _upcoming30DaysCount;
        private set
        {
            if (SetProperty(ref _upcoming30DaysCount, value))
            {
                RaisePropertyChanged(nameof(Upcoming30DaysCountLabel));
            }
        }
    }

    public string Upcoming30DaysCountLabel => $"{Upcoming30DaysCount} {LocalizationCatalog.Get("AnalyticsChargesSuffix")}";

    public override async Task RefreshAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

        IsBusy = true;
        try
        {
            Summary = await dashboardService.GetAsync();
            var subscriptions = await subscriptionService.GetAllAsync();

            _potentialSavings = subscriptions
                .Where(static item => item.IsActive)
                .OrderByDescending(static item => item.MonthlyCostInBaseCurrency)
                .Take(3)
                .Sum(static item => item.MonthlyCostInBaseCurrency);

            TopSubscriptions = subscriptions
                .Where(static item => item.IsActive)
                .OrderByDescending(static item => item.MonthlyCostInBaseCurrency)
                .Take(5)
                .Select((item, index) => new AnalyticsTopSubscriptionItem
                {
                    Rank = index + 1,
                    Name = item.Name,
                    CategoryName = item.CategoryName,
                    MonthlyCostLabel = item.MonthlyCostLabel,
                    ShareLabel = Summary.MonthlyTotal == 0m
                        ? "0%"
                        : $"{item.MonthlyCostInBaseCurrency / Summary.MonthlyTotal * 100m:0.#}%"
                })
                .ToArray();

            BuildUpcomingCharges(subscriptions);
            RebuildCharts();
            RaisePropertyChanged(nameof(PotentialSavingsLabel));
            RaisePropertyChanged(nameof(Upcoming30DaysCountLabel));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildUpcomingCharges(IReadOnlyList<SubscriptionListItemDto> subscriptions)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var rangeEnd = today.AddDays(30);
        var rates = _appSettingsService.GetSettings().ExchangeRatesToRub;
        var charges = new List<(DateOnly PaymentDate, string Name, string Category, decimal Amount, string BaseCurrency)>();

        foreach (var subscription in subscriptions.Where(static item => item.IsActive))
        {
            var occurrence = subscription.NextPaymentDate;
            while (occurrence <= rangeEnd)
            {
                if (occurrence >= today)
                {
                    charges.Add((
                        occurrence,
                        subscription.Name,
                        subscription.CategoryName,
                        CurrencyConverter.Convert(subscription.Amount, subscription.Currency, Summary.BaseCurrency, rates),
                        Summary.BaseCurrency));
                }

                occurrence = RecurringPaymentCalculator.GetNextDate(occurrence, subscription.BillingCycle);
            }
        }

        UpcomingCharges = charges
            .OrderBy(static item => item.PaymentDate)
            .ThenBy(static item => item.Name)
            .Take(5)
            .Select(item => new AnalyticsUpcomingChargeItem
            {
                SubscriptionName = item.Name,
                CategoryName = item.Category,
                DateLabel = item.PaymentDate.ToString("dd.MM.yyyy"),
                AmountLabel = $"{item.Amount:N2} {item.BaseCurrency}"
            })
            .ToArray();

        Upcoming30DaysCount = charges.Count;
        Upcoming30DaysTotalLabel = $"{charges.Sum(static item => item.Amount):N2} {Summary.BaseCurrency}";

        var buckets = charges
            .GroupBy(static item => item.PaymentDate)
            .OrderBy(static item => item.Key)
            .Select(group => new
            {
                Label = group.Key.ToString("dd MMM"),
                Amount = group.Sum(static item => item.Amount)
            })
            .ToArray();

        var accentAlt = GetPaint("AppAccentAltBrush");
        var border = GetPaint("AppBorderBrush", 1, 70);
        var foreground = GetPaint("AppForegroundBrush");
        var muted = GetPaint("AppMutedBrush");

        UpcomingSeries =
        [
            new ColumnSeries<decimal>
            {
                Values = buckets.Select(static item => item.Amount).ToArray(),
                Fill = accentAlt,
                Stroke = null,
                MaxBarWidth = 26
            }
        ];

        UpcomingXAxes =
        [
            new Axis
            {
                Labels = buckets.Select(static item => item.Label).ToArray(),
                LabelsPaint = muted,
                SeparatorsPaint = null,
                TextSize = 12
            }
        ];

        UpcomingYAxes =
        [
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = foreground,
                SeparatorsPaint = border,
                TextSize = 12,
                Labeler = value => value == 0 ? "0" : $"{value:N0}"
            }
        ];
    }

    private void RebuildCharts()
    {
        var accent = GetPaint("AppAccentBrush");
        var foreground = GetPaint("AppForegroundBrush");
        var muted = GetPaint("AppMutedBrush");
        var border = GetPaint("AppBorderBrush", 1, 70);
        var panel = GetPaint("AppSecondaryCardBrush", 2);

        ForecastSeries =
        [
            new ColumnSeries<decimal>
            {
                Values = Summary.MonthlyForecast.Select(static point => point.AmountInBaseCurrency).ToArray(),
                Fill = accent,
                Stroke = null,
                MaxBarWidth = 32
            }
        ];

        ForecastXAxes =
        [
            new Axis
            {
                Labels = Summary.MonthlyForecast.Select(static point => point.MonthLabel).ToArray(),
                LabelsPaint = muted,
                SeparatorsPaint = null,
                TextSize = 12
            }
        ];

        ForecastYAxes =
        [
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = foreground,
                SeparatorsPaint = border,
                TextSize = 12,
                Labeler = value => value == 0 ? "0" : $"{value:N0}"
            }
        ];

        CategorySeries = Summary.CategoryExpenses
            .Select(item => new PieSeries<double>
            {
                Values = [Convert.ToDouble(item.AmountInBaseCurrency)],
                Name = item.CategoryName,
                Fill = new SolidColorPaint(ParseColor(item.ColorHex)),
                Stroke = panel
            })
            .Cast<ISeries>()
            .ToArray();

        if (UpcomingSeries.Length == 0)
        {
            UpcomingSeries =
            [
                new ColumnSeries<decimal>
                {
                    Values = [],
                    Fill = accent,
                    Stroke = null
                }
            ];
        }
    }

    private static SolidColorPaint GetPaint(string resourceKey, float strokeThickness = 0, byte alpha = 255)
    {
        var brush = (SolidColorBrush)System.Windows.Application.Current.Resources[resourceKey];
        var color = brush.Color;
        return new SolidColorPaint(new SKColor(color.R, color.G, color.B, alpha), strokeThickness);
    }

    private static SKColor ParseColor(string colorHex)
    {
        var color = (Color)ColorConverter.ConvertFromString(colorHex);
        return new SKColor(color.R, color.G, color.B, color.A);
    }
}
