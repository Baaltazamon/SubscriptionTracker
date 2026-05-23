using System.Windows;
using System.Windows.Input;

namespace SubscriptionTracker.Wpf.Dialogs;

public partial class ImportPreviewWindow : Window
{
    public ImportPreviewWindow()
    {
        InitializeComponent();
    }

    private void ImportClick(object sender, RoutedEventArgs e)
    {
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
