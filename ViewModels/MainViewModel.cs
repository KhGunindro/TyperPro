using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TyperPro.Services;

namespace TyperPro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly TypingEngineService _engine = new();

    [ObservableProperty]
    private string targetText = TypingEngineService.GetRandomText();

    [ObservableProperty]
    private string inputText = string.Empty;

    [ObservableProperty]
    private double wpm;

    [ObservableProperty]
    private double accuracy;

    [RelayCommand]
    private void Start()
    {
        InputText = string.Empty;
        TargetText = TypingEngineService.GetRandomText();
        _engine.Start(TargetText);
    }

    [RelayCommand]
    private void Reset()
    {
        InputText = string.Empty;
        Wpm = 0;
        Accuracy = 0;
        _engine.Reset();
    }

    partial void OnInputTextChanged(string value)
    {
        Wpm = Math.Round(_engine.CalculateWpm(value.Length), 1);
        Accuracy = Math.Round(_engine.CalculateAccuracy(value), 1);
    }
}