using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TyperPro.Services;
using Avalonia.Media;
using TyperPro.Models;
using System.Collections.ObjectModel;
using Avalonia.Threading;

namespace TyperPro.ViewModels;

public partial class MainViewModel : ObservableObject
{	
    private readonly TypingSoundService _sound = new();
    
    private readonly TypingEngineService _engine = new();
    private DispatcherTimer? _timer;

    [ObservableProperty]
    private string targetText = TypingEngineService.GetRandomText();

    [ObservableProperty]
    private string inputText = string.Empty;

    [ObservableProperty]
    private double wpm;

    [ObservableProperty]
    private double accuracy;

    [ObservableProperty]
    private ObservableCollection<FormattedChar> formattedText = new();

    [ObservableProperty]
    private int remainingSeconds = TypingEngineService.TestDurationSeconds;

    // =========================
    // Commands
    // =========================

    [RelayCommand]
    private void Start()
    {
        InputText = string.Empty;
        TargetText = TypingEngineService.GetRandomText();

        _engine.Start(TargetText);

        RemainingSeconds = TypingEngineService.TestDurationSeconds;
        Wpm = 0;
        Accuracy = 0;

        UpdateFormattedText();

        _timer?.Stop();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _timer.Tick += (_, _) =>
        {
            RemainingSeconds = _engine.RemainingSeconds;

            if (RemainingSeconds <= 0)
            {
                _engine.Stop();
                _timer.Stop();
            }
        };

        _timer.Start();
    }

    [RelayCommand]
    private void Reset()
    {
        _timer?.Stop();
        _engine.Reset();

        InputText = string.Empty;
        Wpm = 0;
        Accuracy = 0;
        RemainingSeconds = TypingEngineService.TestDurationSeconds;

        UpdateFormattedText();
    }

    // =========================
    // Typing logic
    // =========================

    partial void OnInputTextChanged(string value)
    {
        if (!_engine.IsRunning)
            return;
        
        _sound.Play();

        Wpm = Math.Round(_engine.CalculateWpm(value), 1);
        Accuracy = Math.Round(_engine.CalculateAccuracy(value), 1);

        UpdateFormattedText();
    }

    // =========================
    // Highlight + caret logic
    // =========================

    private void UpdateFormattedText()
    {
        FormattedText.Clear();

        for (int i = 0; i < TargetText.Length; i++)
        {
            bool isTyped = i < InputText.Length;
            bool isCaret = i == InputText.Length && _engine.IsRunning;

            IBrush brush;

            if (isTyped)
            {
                brush = InputText[i] == TargetText[i]
                    ? Brushes.LimeGreen
                    : Brushes.IndianRed;
            }
            else
            {
                brush = Brushes.Gray;
            }

            FormattedText.Add(
                new FormattedChar(
                    TargetText[i],
                    brush,
                    isTyped,
                    isCaret
                )
            );
        }
    }
}
