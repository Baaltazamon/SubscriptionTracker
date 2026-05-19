using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SubscriptionTracker.Application.Localization;

namespace SubscriptionTracker.Wpf.Dialogs;

public partial class DialogWindow : Window
{
    private readonly DialogButton _primaryButton;
    private readonly DialogButton? _secondaryButton;

    public DialogWindow(DialogRequest request)
    {
        _primaryButton = request.PrimaryButton;
        _secondaryButton = request.SecondaryButton;

        DialogTitle = request.Title;
        DialogMessage = request.Message;
        KindGlyph = GetKindGlyph(request.Kind);
        KindCaption = GetKindCaption(request.Kind);
        AccentBrush = GetAccentBrush(request.Kind);
        PrimaryButtonText = GetButtonText(request.PrimaryButton);
        SecondaryButtonText = request.SecondaryButton is null ? string.Empty : GetButtonText(request.SecondaryButton.Value);
        SecondaryButtonVisibility = request.SecondaryButton is null ? Visibility.Collapsed : Visibility.Visible;
        PrimaryButtonStyle = GetPrimaryStyle(request.PrimaryButton);
        SecondaryButtonStyle = GetSecondaryStyle();

        DataContext = this;
        InitializeComponent();
    }

    public DialogResultKind Result { get; private set; } = DialogResultKind.None;

    public string DialogTitle { get; }

    public string DialogMessage { get; }

    public string KindGlyph { get; }

    public string KindCaption { get; }

    public Brush AccentBrush { get; }

    public string PrimaryButtonText { get; }

    public string SecondaryButtonText { get; }

    public Visibility SecondaryButtonVisibility { get; }

    public Style PrimaryButtonStyle { get; }

    public Style SecondaryButtonStyle { get; }

    private void PrimaryClick(object sender, RoutedEventArgs e)
    {
        Result = MapButton(_primaryButton);
        Close();
    }

    private void SecondaryClick(object sender, RoutedEventArgs e)
    {
        Result = MapButton(_secondaryButton ?? DialogButton.Cancel);
        Close();
    }

    private void HeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (Result == DialogResultKind.None && _secondaryButton is not null)
        {
            Result = MapButton(_secondaryButton.Value);
        }

        base.OnClosing(e);
    }

    private static Brush GetAccentBrush(DialogKind kind)
    {
        return kind switch
        {
            DialogKind.Error => GetThemeBrush("AppDangerBrush"),
            DialogKind.Confirm => GetThemeBrush("AppAccentAltBrush"),
            _ => GetThemeBrush("AppAccentBrush")
        };
    }

    private static Style GetPrimaryStyle(DialogButton button)
    {
        var key = button == DialogButton.Delete ? "DangerButtonStyle" : "PrimaryButtonStyle";
        return (Style)System.Windows.Application.Current.Resources[key];
    }

    private static Style GetSecondaryStyle()
    {
        return (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"];
    }

    private static Brush GetThemeBrush(string key)
    {
        return (Brush)System.Windows.Application.Current.Resources[key];
    }

    private static string GetKindGlyph(DialogKind kind)
    {
        return kind switch
        {
            DialogKind.Error => "×",
            DialogKind.Warning => "!",
            DialogKind.Confirm => "?",
            _ => "i"
        };
    }

    private static string GetKindCaption(DialogKind kind)
    {
        return kind switch
        {
            DialogKind.Error => LocalizationCatalog.Get("DialogKindError"),
            DialogKind.Warning => LocalizationCatalog.Get("DialogKindWarning"),
            DialogKind.Confirm => LocalizationCatalog.Get("DialogKindConfirm"),
            _ => LocalizationCatalog.Get("DialogKindInfo")
        };
    }

    private static string GetButtonText(DialogButton button)
    {
        return button switch
        {
            DialogButton.Cancel => LocalizationCatalog.Get("DialogCancel"),
            DialogButton.Delete => LocalizationCatalog.Get("DialogDelete"),
            DialogButton.Restore => LocalizationCatalog.Get("DialogRestore"),
            _ => LocalizationCatalog.Get("DialogOk")
        };
    }

    private static DialogResultKind MapButton(DialogButton button)
    {
        return button switch
        {
            DialogButton.Ok => DialogResultKind.Ok,
            DialogButton.Cancel => DialogResultKind.Cancel,
            DialogButton.Delete => DialogResultKind.Delete,
            DialogButton.Restore => DialogResultKind.Restore,
            _ => DialogResultKind.None
        };
    }
}
