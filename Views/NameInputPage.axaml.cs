using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace TyperPro.Views;

public partial class NameInputPage : UserControl
{
    public event Action<string>? OnNameSubmitted;

    public NameInputPage()
    {
        InitializeComponent();
        StartButton.Click    += (_, _) => TrySubmit();
        NameTextBox.KeyDown  += OnKeyDown;
    }

    public void FocusInput() => NameTextBox.Focus();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TrySubmit();
    }

    private void TrySubmit()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ErrorText.IsVisible = true;
            NameTextBox.Focus();
            return;
        }
        OnNameSubmitted?.Invoke(NameTextBox.Text.Trim());
    }
}