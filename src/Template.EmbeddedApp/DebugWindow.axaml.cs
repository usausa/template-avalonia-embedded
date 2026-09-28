namespace Template.EmbeddedApp;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

public partial class DebugWindow : Window
{
    private DebugWindowViewModel? ViewModel => DataContext as DebugWindowViewModel;

    public DebugWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnWindowKeyUp, RoutingStrategies.Tunnel);
        Deactivated += (_, _) => ViewModel?.ReleaseAll();
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if ((ViewModel is { } viewModel) && (viewModel.FindKey(e.Key) is { } key))
        {
            viewModel.Press(key);
            e.Handled = true;
        }
    }

    private void OnWindowKeyUp(object? sender, KeyEventArgs e)
    {
        if ((ViewModel is { } viewModel) && (viewModel.FindKey(e.Key) is { } key))
        {
            viewModel.Release(key);
            e.Handled = true;
        }
    }

    private void OnKeyPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((ViewModel is { } viewModel) && (sender is Control { DataContext: DebugKey key } control))
        {
            e.Pointer.Capture(control);
            viewModel.Press(key);
            e.Handled = true;
        }
    }

    private void OnKeyPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if ((ViewModel is { } viewModel) && (sender is Control { DataContext: DebugKey key }))
        {
            e.Pointer.Capture(null);
            viewModel.Release(key);
            e.Handled = true;
        }
    }

    private void OnKeyPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if ((ViewModel is { } viewModel) && (sender is Control { DataContext: DebugKey key }))
        {
            viewModel.Release(key);
        }
    }
}
