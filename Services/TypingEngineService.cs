using System.Diagnostics;

namespace TyperPro.Services;
public class TypingEngineService
{
    private readonly Stopwatch _stopwatch = new();
    private string _targetText = string.Empty;
    private int _totalKeystrokes;

    public const int TestDurationSeconds = 30;

    public bool IsRunning => _stopwatch.IsRunning;
    public int ElapsedSeconds => (int)_stopwatch.Elapsed.TotalSeconds;
    public int RemainingSeconds => Math.Max(0, TestDurationSeconds - ElapsedSeconds);
    public string TargetText => _targetText;

    // 🔥 ONLY sets text
    public void SetTargetText(string targetText)
    {
        _targetText = targetText;
        ResetTimer();
    }

    // 🔥 PURE timer reset
    public void ResetTimer()
    {
        _stopwatch.Reset();
        _totalKeystrokes = 0;
    }

    // 🔥 NO target text reset here
    public void Reset()
    {
        ResetTimer();
    }

    public void Start()
    {
        _stopwatch.Reset();  
        if (!_stopwatch.IsRunning)
            _stopwatch.Start();
    }

    public void Stop()
    {
        if (_stopwatch.IsRunning)
            _stopwatch.Stop();
    }

    public void RegisterKeystroke() => _totalKeystrokes++;

    public int CountCorrectChars(string input)
    {
        int correct = 0;
        int len = Math.Min(input.Length, _targetText.Length);
        for (int i = 0; i < len; i++)
            if (input[i] == _targetText[i]) correct++;
        return correct;
    }

    public double CalculateWpm(string input)
    {
        if (!IsRunning) return 0;
        double minutes = _stopwatch.Elapsed.TotalMinutes;
        if (minutes <= 0) return 0;
        int correctChars = CountCorrectChars(input);
        return (correctChars / 5.0) / minutes;
    }

    public double CalculateRawWpm()
    {
        if (!IsRunning) return 0;
        double minutes = _stopwatch.Elapsed.TotalMinutes;
        if (minutes <= 0) return 0;
        return (_totalKeystrokes / 5.0) / minutes;
    }

    public double CalculateAccuracy(string input)
    {
        if (input.Length == 0) return 0;
        int correct = CountCorrectChars(input);
        return (double)correct / input.Length * 100;
    }
}
