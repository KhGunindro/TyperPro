using Avalonia;
using TyperPro.Services;
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
            .LogToTrace();
}