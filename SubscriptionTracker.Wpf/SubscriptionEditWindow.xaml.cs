using System.Windows;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Wpf.ViewModels;
using SubscriptionTracker.Wpf.Services;

namespace SubscriptionTracker.Wpf;

public partial class SubscriptionEditWindow : Window
{
    private readonly IDialogService _dialogService;

    public SubscriptionEditWindow(IDialogService dialogService)
    {
        _dialogService = dialogService;
        InitializeComponent();
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SubscriptionEditViewModel viewModel)
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
