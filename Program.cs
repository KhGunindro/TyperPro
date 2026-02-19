using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using TyperPro.Services;
using TyperPro.Views;
using System;

namespace TyperPro;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        DatabaseService.Initialize();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .AfterSetup(async x =>
            {
                if (x.Instance?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // Temporary invisible window as owner
                    var tempWindow = new Window
                    {
                        Width = 0,
                        Height = 0,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        SystemDecorations = SystemDecorations.None,
                        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
                        Background = Brushes.Transparent,
                        IsVisible = true
                    };
                    desktop.MainWindow = tempWindow;
                    tempWindow.Show();

                    var dialog = new NameInputDialog();
                    var playerName = await dialog.ShowDialog<string?>(tempWindow);

                    if (string.IsNullOrEmpty(playerName))
                    {
                        desktop.Shutdown();
                    }
                    else
                    {
                        var mainWindow = new MainWindow(playerName);
                        desktop.MainWindow = mainWindow;
                        tempWindow.Close();
                        mainWindow.Show();
                    }
                }
            });
}