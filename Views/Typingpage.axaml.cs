using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TyperPro.Views;

public partial class TypingPage : UserControl
{
    public TypingPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}