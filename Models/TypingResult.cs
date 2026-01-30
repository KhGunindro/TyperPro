namespace TyperPro.Models;

public class TypingResult
{
    public double Wpm { get; init; }
    public double Accuracy { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}