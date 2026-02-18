using Avalonia;
using System.Collections.Generic;

namespace TyperPro.ViewModels;

public class ResultViewModel
{
    public double Wpm { get; set; }
    public double Accuracy { get; set; }
    public double RawWpm { get; set; }
    public int CorrectChars { get; set; }
    public int IncorrectChars { get; set; }
    public int MissedChars { get; set; }
    public int ExtraChars { get; set; }
    public double Consistency { get; set; }
    public string TestType { get; set; } = "time 30";
    public string Time { get; set; } = "30s";
    public string SessionDuration { get; set; } = "00:00:00";
    public List<Point> WpmPoints { get; set; } = new();
    public List<Point> RawWpmPoints { get; set; } = new();
    public List<Point> ErrorPoints { get; set; } = new();
}