using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using TyperPro.Models;

namespace TyperPro.ViewModels;

public class SummaryViewModel
{
    public string PlayerName { get; }
    public ObservableCollection<RoundSummary> RoundSummaries { get; } = new();
    public List<RoundResult> AllResults { get; }

    public double OverallAvgWpm      { get; }
    public double OverallAvgRaw      { get; }
    public double OverallAvgAccuracy { get; }
    public double OverallBestWpm     { get; }

    public List<Point> AverageWpmPoints   { get; }
    public List<Point> AverageErrorPoints { get; }

    public SummaryViewModel(List<RoundSummary> summaries, string playerName)
    {
        PlayerName = playerName;
        foreach (var s in summaries) RoundSummaries.Add(s);
        AllResults = summaries.SelectMany(s => s.SubRounds).ToList();

        if (AllResults.Count > 0)
        {
            OverallAvgWpm      = AllResults.Average(r => r.Wpm);
            OverallAvgRaw      = AllResults.Average(r => r.RawWpm);
            OverallAvgAccuracy = AllResults.Average(r => r.Accuracy);
            OverallBestWpm     = AllResults.Max(r => r.Wpm);
        }

        int maxSeconds = AllResults.Any() ? AllResults.Max(r => r.WpmPoints.Count) : 0;
        var avgWpm   = new List<Point>();
        var avgError = new List<Point>();

        for (int i = 0; i < maxSeconds; i++)
        {
            double sumWpm = 0, sumErr = 0;
            int cntWpm = 0, cntErr = 0;
            foreach (var r in AllResults)
            {
                if (i < r.WpmPoints.Count)   { sumWpm += r.WpmPoints[i].Y;   cntWpm++; }
                if (i < r.ErrorPoints.Count) { sumErr += r.ErrorPoints[i].Y; cntErr++; }
            }
            if (cntWpm > 0) avgWpm.Add(new Point(i,   sumWpm / cntWpm));
            if (cntErr > 0) avgError.Add(new Point(i, sumErr / cntErr));
        }

        AverageWpmPoints   = avgWpm;
        AverageErrorPoints = avgError;
    }
}