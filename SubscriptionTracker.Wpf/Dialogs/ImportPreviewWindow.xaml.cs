using System.Windows;
using System.Windows.Input;
using SubscriptionTracker.Wpf.ViewModels;

namespace SubscriptionTracker.Wpf.Dialogs;

public partial class ImportPreviewWindow : Window
{
    public ImportPreviewWindow()
    {
        InitializeComponent();
    }

    private void ImportClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ImportPreviewViewModel viewModel || !viewModel.CanImport)
        {
            return;
        }

        DialogResult = true;
        Close();
    }

    private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
