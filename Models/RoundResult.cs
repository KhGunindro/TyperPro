using System.Collections.Generic;
using Avalonia;

namespace TyperPro.Models;

public class RoundResult
{
    public string RoundName     { get; set; } = string.Empty; // "Easy", "Medium", "Hard"
    public int    SubRoundIndex { get; set; }                  // 0, 1, 2
    public double Wpm           { get; set; }
    public double Accuracy      { get; set; }
    public double RawWpm        { get; set; }
    public int    CorrectChars  { get; set; }
    public int    IncorrectChars{ get; set; }
    public int    MissedChars   { get; set; }
    public int    ExtraChars    { get; set; }
    public double Consistency   { get; set; }
    public int    AttemptsUsed  { get; set; }
    public List<Point> WpmPoints    { get; set; } = new();
    public List<Point> RawWpmPoints { get; set; } = new();
    public List<Point> ErrorPoints  { get; set; } = new();
}