using System.Diagnostics;

namespace TyperPro.Services;

public class TypingEngineService
{
    private readonly Stopwatch _stopwatch = new();
    private string _targetText = string.Empty;

    public const int TestDurationSeconds = 60;

    public bool IsRunning => _stopwatch.IsRunning;

    public int ElapsedSeconds =>
        (int)_stopwatch.Elapsed.TotalSeconds;

    public int RemainingSeconds =>
        Math.Max(0, TestDurationSeconds - ElapsedSeconds);

    public void Start(string targetText)
    {
        _targetText = targetText;
        _stopwatch.Restart();
    }

    public void Stop()
    {
        _stopwatch.Stop();
    }

    public void Reset()
    {
        _stopwatch.Reset();
        _targetText = string.Empty;
    }

    public int CountCorrectChars(string input)
    {
        int correct = 0;
        int len = Math.Min(input.Length, _targetText.Length);

        for (int i = 0; i < len; i++)
        {
            if (input[i] == _targetText[i])
                correct++;
        }

        return correct;
    }

    public double CalculateWpm(string input)
    {
        if (!_stopwatch.IsRunning)
            return 0;

        double minutes = _stopwatch.Elapsed.TotalMinutes;
        if (minutes <= 0)
            return 0;

        int correctChars = CountCorrectChars(input);

        // Standard net WPM formula
        return (correctChars / 5.0) / minutes;
    }

    public double CalculateAccuracy(string input)
    {
        if (input.Length == 0)
            return 0;

        int correct = CountCorrectChars(input);
        return (double)correct / input.Length * 100;
    }

    public static string GetRandomText()
    {
        string[] samples =
        {
            "The quick brown fox jumps over the lazy dog.",
            "Typing speed improves with practice and consistency.",
            "Avalonia UI makes cross platform desktop apps possible.",
            "Cybrella 2026 is going to be amazing."
        };

        return samples[Random.Shared.Next(samples.Length)];
    }
}
