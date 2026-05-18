using System.Windows;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Wpf.Services;
using SubscriptionTracker.Wpf.ViewModels;

namespace SubscriptionTracker.Wpf.Dialogs;

public partial class CategoryDeleteWindow : Window
{
    private readonly IDialogService _dialogService;

    public CategoryDeleteWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        InitializeComponent();
    }

    private void DeleteClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CategoryDeleteViewModel viewModel)
        {
            return;
        }

        var validationError = viewModel.Validate();
        if (validationError is not null)
        {
            _dialogService.ShowWarning(validationError, LocalizationCatalog.Get("ValidateDataTitle"));
            return;
        }

        DialogResult = true;
        Close();
    }
}
