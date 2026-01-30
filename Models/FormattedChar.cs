using Avalonia.Media;

namespace TyperPro.Models;

public class FormattedChar
{
    public char Character { get; }
    public IBrush Foreground { get; }

    public FormattedChar(char character, IBrush foreground)
    {
        Character = character;
        Foreground = foreground;
    }
}