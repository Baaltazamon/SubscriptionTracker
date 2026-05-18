using System.Windows;
using SubscriptionTracker.Application.Localization;
using SubscriptionTracker.Wpf.ViewModels;

namespace SubscriptionTracker.Wpf;

public partial class SubscriptionEditWindow : Window
{
    public SubscriptionEditWindow()
    {
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
            MessageBox.Show(validationError, LocalizationCatalog.Get("ValidateDataTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
