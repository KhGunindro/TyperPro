using Avalonia.Media;

namespace TyperPro.Models;

public class FormattedChar
{
    public string Character { get; set; } = string.Empty;
    public string Foreground { get; set; } = string.Empty;
    public bool IsCaret { get; set; }
}