using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TyperPro.Views;

namespace TyperPro;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}