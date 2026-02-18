using Avalonia.Media;

namespace TyperPro.Models;

public class FormattedChar
{
    public char Character { get; }
    public IBrush Foreground { get; }
    public bool IsTyped { get; }
    public bool IsCaret { get; }

    public FormattedChar(
        char character,
        IBrush foreground,
        bool isTyped,
        bool isCaret)
    {
        Character = character;
        Foreground = foreground;
        IsTyped = isTyped;
        IsCaret = isCaret;
    }
}