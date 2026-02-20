using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TyperPro.Models;

public class FormattedChar : INotifyPropertyChanged
{
    private string _character  = string.Empty;
    private string _foreground = "#808080";
    private bool   _isCaret;

    public string Character
    {
        get => _character;
        set { _character = value; OnPropertyChanged(); }
    }

    public string Foreground
    {
        get => _foreground;
        set { _foreground = value; OnPropertyChanged(); }
    }

    public bool IsCaret
    {
        get => _isCaret;
        set { _isCaret = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}