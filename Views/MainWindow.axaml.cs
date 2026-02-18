using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using TyperPro.ViewModels;

namespace TyperPro.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => Focus();

        if (DataContext is MainViewModel vm)
        {
            vm.ShowResultRequested += OnShowResultRequested;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.HandleKey(e);
            e.Handled = true;
        }
    }

    private async void OnShowResultRequested(object? sender, ResultEventArgs e)
    {
        var dialog = new ResultDialog
        {
            DataContext = e.Result
        };
        await dialog.ShowDialog(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowResultRequested -= OnShowResultRequested;
            vm.Dispose();
        }
        base.OnClosed(e);
    }
}