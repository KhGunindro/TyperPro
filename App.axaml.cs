using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
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
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Called by Avalonia for every raw key event in the entire application,
    /// before any window or control sees it. We forward to MainWindow if it exists.
    /// This completely bypasses focus — it doesn't matter what has focus.
    /// </summary>
    public void OnGlobalKeyDown(Key key)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is MainWindow mw)
        {
            if (key == Key.Back)
                mw.HandleBackspace();
        }
    }

    public void OnGlobalTextInput(string text)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is MainWindow mw)
            mw.HandleCharacter(text);
    }
}