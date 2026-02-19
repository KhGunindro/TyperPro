using System.Collections.Generic;
using System.Linq;

namespace TyperPro.Models;

public class RoundSummary
{
    public string           RoundName   { get; set; } = string.Empty;
    public List<RoundResult> SubRounds  { get; set; } = new();

    // Computed averages
    public double AvgWpm      => SubRounds.Count > 0 ? SubRounds.Average(r => r.Wpm)      : 0;
    public double AvgRawWpm   => SubRounds.Count > 0 ? SubRounds.Average(r => r.RawWpm)   : 0;
    public double AvgAccuracy => SubRounds.Count > 0 ? SubRounds.Average(r => r.Accuracy) : 0;
    public double BestWpm     => SubRounds.Count > 0 ? SubRounds.Max(r => r.Wpm)          : 0;
    public int    TotalCorrect   => SubRounds.Sum(r => r.CorrectChars);
    public int    TotalIncorrect => SubRounds.Sum(r => r.IncorrectChars);
}