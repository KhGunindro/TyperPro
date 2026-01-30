using System.Diagnostics;

namespace TyperPro.Services;

public class TypingEngineService
{
    private readonly Stopwatch _stopwatch = new();
    private string _targetText = string.Empty;

    public void Start(string targetText)
    {
        _targetText = targetText;
        _stopwatch.Restart();
    }

    public void Reset()
    {
        _stopwatch.Reset();
        _targetText = string.Empty;
    }

    public double CalculateWpm(int totalTypedCharacters)
    {
        if (!_stopwatch.IsRunning || _stopwatch.Elapsed.TotalMinutes <= 0)
            return 0;

        return (totalTypedCharacters / 5.0) / _stopwatch.Elapsed.TotalMinutes;
    }

    public double CalculateAccuracy(string input)
    {
        if (string.IsNullOrEmpty(_targetText))
            return 0;

        int correct = 0;
        int length = Math.Min(input.Length, _targetText.Length);

        for (int i = 0; i < length; i++)
        {
            if (input[i] == _targetText[i])
                correct++;
        }

        return input.Length == 0
            ? 0
            : (double)correct / input.Length * 100;
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