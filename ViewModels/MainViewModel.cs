using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using TyperPro.Models;
using TyperPro.Services;

namespace TyperPro.ViewModels;

public class ResultEventArgs : EventArgs
{
    public ResultViewModel Result { get; set; }
}

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly TypingEngineService _engine = new();
    private readonly TypingSoundService _sound = new();
    private string _targetText = string.Empty;
    private string _typedText = string.Empty;
    private List<double> _wpmSamples = new();
    private List<double> _rawWpmSamples = new();
    private List<double> _errorSamples = new();
    private DateTime _testStartTime;
    private bool _resultShown;

    [ObservableProperty]
    private ObservableCollection<FormattedChar> _formattedText = new();

    [ObservableProperty]
    private double _wpm;

    [ObservableProperty]
    private double _accuracy;

    [ObservableProperty]
    private int _remainingSeconds;

    [ObservableProperty]
    private int _currentIndex;

    private System.Timers.Timer? _timer;

    public event EventHandler<ResultEventArgs>? ShowResultRequested;

    public MainViewModel()
    {
        StartCommand = new RelayCommand(Start);
        ResetCommand = new RelayCommand(Reset);
    }

    public IRelayCommand StartCommand { get; }
    public IRelayCommand ResetCommand { get; }

    private void Start()
    {
        _targetText = TypingEngineService.GetRandomText();
        _resultShown = false;
        _typedText = string.Empty;
        CurrentIndex = 0;
        _engine.Start(_targetText);
        UpdateDisplay();

        _wpmSamples.Clear();
        _rawWpmSamples.Clear();
        _errorSamples.Clear();
        _testStartTime = DateTime.Now;

        _timer?.Dispose();
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += UpdateStats;
        _timer.AutoReset = true;
        _timer.Start();
    }

    private void Reset()
    {
        _engine.Reset();
        _typedText = string.Empty;
        CurrentIndex = 0;
        _targetText = string.Empty;
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        UpdateDisplay();
        Wpm = 0;
        Accuracy = 0;
        RemainingSeconds = TypingEngineService.TestDurationSeconds;
    }

    private void UpdateStats(object? sender, ElapsedEventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            RemainingSeconds = _engine.RemainingSeconds;
            Wpm = _engine.CalculateWpm(_typedText);
            Accuracy = _engine.CalculateAccuracy(_typedText);

            _wpmSamples.Add(Wpm);
            double minutes = _engine.ElapsedSeconds / 60.0;
            double rawWpm = minutes > 0 ? _typedText.Length / 5.0 / minutes : 0;
            _rawWpmSamples.Add(rawWpm);
            int correct = _engine.CountCorrectChars(_typedText);
            int incorrect = Math.Max(0, _typedText.Length - correct);
            _errorSamples.Add(incorrect);

            if (RemainingSeconds <= 0)
            {
                _timer?.Stop();
                _timer?.Dispose();
                _timer = null;
                ShowResult();
            }
        });
    }

    private void ShowResult()
    {   
        if (_resultShown) return;
        _resultShown = true;
        var result = new ResultViewModel();

        result.Wpm = Wpm;
        result.Accuracy = Accuracy;

        double minutes = _engine.ElapsedSeconds / 60.0;
        result.RawWpm = minutes > 0 ? _typedText.Length / 5.0 / minutes : 0;

        int correct = _engine.CountCorrectChars(_typedText);
        int incorrect = Math.Max(0, _typedText.Length - correct);
        int missed = Math.Max(0, _targetText.Length - _typedText.Length);
        int extra = Math.Max(0, _typedText.Length - _targetText.Length);
        result.CorrectChars = correct;
        result.IncorrectChars = incorrect;
        result.MissedChars = missed;
        result.ExtraChars = extra;

        if (_wpmSamples.Count > 1)
        {
            double avg = _wpmSamples.Average();
            double stdDev = Math.Sqrt(_wpmSamples.Select(v => Math.Pow(v - avg, 2)).Average());
            result.Consistency = avg > 0 ? (1 - stdDev / avg) * 100 : 0;
        }
        else
        {
            result.Consistency = 100;
        }

        var elapsed = DateTime.Now - _testStartTime;
        result.SessionDuration = elapsed.ToString(@"hh\:mm\:ss");

        int totalSeconds = _wpmSamples.Count;
        if (totalSeconds > 0)
        {
            double maxWpm = Math.Max(_wpmSamples.Max(), _rawWpmSamples.Max()) * 1.1;
            for (int i = 0; i < totalSeconds; i++)
            {
                double x = (double)i / totalSeconds * 100;
                result.WpmPoints.Add(new Point(x, _wpmSamples[i] / maxWpm * 100));
                result.RawWpmPoints.Add(new Point(x, _rawWpmSamples[i] / maxWpm * 100));
                result.ErrorPoints.Add(new Point(x, _errorSamples[i] / 10.0 * 100));
            }
        }

        ShowResultRequested?.Invoke(this, new ResultEventArgs { Result = result });
    }

    public void HandleKey(KeyEventArgs e)
    {
        if (e.Key == Key.LeftShift || e.Key == Key.RightShift ||
            e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
            e.Key == Key.LeftAlt || e.Key == Key.RightAlt)
            return;

        if (e.Key == Key.Enter)
        {
            if (_engine.IsRunning)
                ShowResult();
            e.Handled = true;
            return;
        }

        if (!_engine.IsRunning || RemainingSeconds <= 0)
            return;

        if (e.Key != Key.Back)
            _sound.Play();

        if (e.Key == Key.Back)
        {
            if (_typedText.Length > 0)
            {
                _typedText = _typedText[..^1];
                CurrentIndex = Math.Max(0, CurrentIndex - 1);
            }
        }
        else if (e.Key >= Key.A && e.Key <= Key.Z)
        {
            bool isShift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
            char ch = (char)(isShift ? 'A' + (e.Key - Key.A) : 'a' + (e.Key - Key.A));
            _typedText += ch;
            CurrentIndex = _typedText.Length;
        }
        else if (e.Key == Key.Space)
        {
            _typedText += ' ';
            CurrentIndex = _typedText.Length;
        }
        else if (e.Key == Key.OemPeriod)
        {
            _typedText += '.';
            CurrentIndex = _typedText.Length;
        }
        // Add more punctuation as needed

        UpdateDisplay();
        RemainingSeconds = _engine.RemainingSeconds;
        Wpm = _engine.CalculateWpm(_typedText);
        Accuracy = _engine.CalculateAccuracy(_typedText);
    }

    private void UpdateDisplay()
    {
        var list = new ObservableCollection<FormattedChar>();

        for (int i = 0; i < _targetText.Length; i++)
        {
            char targetChar = _targetText[i];
            IBrush foreground;

            bool isTyped = i < _typedText.Length;
            bool isCorrect = isTyped && _typedText[i] == targetChar;

            if (isTyped)
                foreground = isCorrect ? new SolidColorBrush(Color.Parse("#4CAF50")) : new SolidColorBrush(Color.Parse("#F44336"));
            else
                foreground = new SolidColorBrush(Color.Parse("#757575"));

            bool isCaret = (i == CurrentIndex) && _engine.IsRunning;

            list.Add(new FormattedChar(targetChar, foreground, isTyped, isCaret));
        }

        FormattedText = list;
    }

    public void Dispose()
    {
        _sound.Dispose();
        _timer?.Dispose();
    }
}