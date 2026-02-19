using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace TyperPro.Views;

public partial class NameInputDialog : Window
{
    public NameInputDialog()
    {
        InitializeComponent();
        Opened += OnOpened;
        NameTextBox.KeyDown += OnKeyDown;
    }

    private void OnOpened(object? sender, System.EventArgs e)
    {
        // Trigger fade-in by setting opacity to 1 after window is shown
        ContentPanel.Opacity = 1;
        NameTextBox.Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TrySubmit();
    }

    private void StartGame_Click(object? sender, RoutedEventArgs e)
        => TrySubmit();

    private void TrySubmit()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ErrorText.IsVisible = true;
            NameTextBox.Focus();
            return;
        }
        Close(NameTextBox.Text.Trim());
    }
}