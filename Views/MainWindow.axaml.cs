using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TyperPro.Models;
using TyperPro.Services;

namespace TyperPro.Views;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly TypingEngineService  _typingEngine;
    private readonly TypingSoundService?  _soundService;
    private readonly string               _playerName;

    private int _roundIndex    = 0;
    private int _subRoundIndex = 0;
    private int _attemptsUsed  = 0;
    private const int MaxAttemptsPerSubRound = 3;

    private readonly List<RoundSummary> _roundSummaries      = new();
    private RoundSummary                _currentRoundSummary = new();

    private string      _currentInput = string.Empty;
    private List<Point> _wpmPoints    = new();
    private List<Point> _rawWpmPoints = new();
    private List<Point> _errorPoints  = new();

    private DispatcherTimer? _roundTimer;
    private DispatcherTimer? _countdownTimer;

    private readonly TypingPage       _typingPage;
    private readonly ResultPage       _resultPage;
    private readonly RoundSummaryPage _roundSummaryPage;

    public IRelayCommand StartCommand { get; }
    public IRelayCommand ResetCommand { get; }

    // ── Backing fields ───────────────────────────────────────────────────────
    private string _roundDisplay     = string.Empty;
    private int    _attemptsLeft;
    private double _wpm;
    private double _accuracy;
    private int    _remainingSeconds;
    private bool   _isCountingDown;
    private int    _countdownValue;
    private ObservableCollection<FormattedChar> _formattedText = new();

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    // ── Paragraphs ───────────────────────────────────────────────────────────
    private static readonly string[][] Paragraphs =
    {
        new[] {
            "The quick brown fox jumps over the lazy dog near the bank of the river. A quiet breeze rustles the leaves and carries the scent of fresh rain. It is a peaceful day in the countryside where animals roam free.",
            "She sells seashells by the seashore and smiles at every passing sailor. The waves crash gently on the rocks as the sun sets behind the distant hills. A lone gull cries out and wheels away into the golden sky.",
            "Tom walked slowly through the old park and watched the pigeons peck at crumbs. Children laughed on the swings while their parents sat on benches reading books. The smell of cut grass lingered pleasantly in the warm afternoon air."
        },
        new[] {
            "In the world of technology, artificial intelligence continues to evolve at a rapid pace. Machine learning algorithms now drive cars, diagnose diseases, and compose music. While the future holds immense promise, we must also consider the ethical implications.",
            "Climate change presents one of the most complex challenges humanity has ever faced. Scientists warn that without immediate action, rising temperatures will displace millions and disrupt food supplies globally. Collaboration between nations is no longer optional but urgent.",
            "The global economy has become deeply interconnected through decades of trade agreements and digital communication. A disruption in one market can ripple across continents within hours. Understanding these dynamics is essential for navigating the modern financial landscape."
        },
        new[] {
            "The intricate dance of quantum mechanics reveals a universe far stranger than fiction. Particles exist in superposition, entangled across vast distances, defying classical logic. As physicists delve deeper into the subatomic realm, they uncover layers of reality that challenge perception.",
            "Epistemological inquiry demands we question the very foundations upon which knowledge is constructed. Kant's transcendental idealism proposed that space and time are cognitive frameworks imposed by the mind. Such radical reconceptions continue to reverberate through contemporary philosophy.",
            "The thermodynamic arrow of time — entropy's inexorable increase — distinguishes past from future in a universe whose fundamental laws are otherwise time-symmetric. This asymmetry, emerging from boundary conditions rather than dynamics, remains one of physics' most provocative open questions."
        }
    };

    // ── Constructor ──────────────────────────────────────────────────────────
    public MainWindow(string playerName)
    {
        _playerName   = playerName;
        _typingEngine = new TypingEngineService();

        try   { _soundService = new TypingSoundService(); }
        catch (Exception ex)
        {
            Console.WriteLine($"[Audio] Sound disabled: {ex.Message}");
            _soundService = null;
        }

        StartCommand = new RelayCommand(StartSubRound, () => !_typingEngine.IsRunning && !_isCountingDown);
        ResetCommand = new RelayCommand(ResetSubRound, () => _typingEngine.IsRunning && _attemptsUsed < MaxAttemptsPerSubRound);

        InitializeComponent();

        _typingPage             = new TypingPage();
        _typingPage.DataContext = this;

        _resultPage              = new ResultPage();
        _resultPage.OnNextRound += GoToNextSubRound;
        _resultPage.OnClose     += ForceFinish;

        _roundSummaryPage              = new RoundSummaryPage();
        _roundSummaryPage.OnNextRound += GoToNextRound;
        _roundSummaryPage.OnFinish    += GoToSummary;

        PageHost.Content = _typingPage;

        // TextInput handles ALL printable chars; KeyDown only for Backspace
        this.AddHandler(InputElement.TextInputEvent, OnWindowTextInput, RoutingStrategies.Tunnel);
        this.AddHandler(InputElement.KeyDownEvent,   OnWindowKeyDown,   RoutingStrategies.Tunnel);

        _currentRoundSummary = new RoundSummary { RoundName = RoundName(_roundIndex) };
        LoadSubRound();
    }

    // ── Window events ────────────────────────────────────────────────────────
    private void OnLoaded(object? sender, RoutedEventArgs e)                 => this.Focus();
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e) => this.Focus();

    // ── Input handlers ───────────────────────────────────────────────────────

    /// All printable characters (letters, digits, space, hyphen, underscore,
    /// punctuation, etc.) arrive here correctly regardless of keyboard layout.
    private void OnWindowTextInput(object? sender, TextInputEventArgs e)
    {
        if (PageHost.Content != _typingPage) return;
        if (!_typingEngine.IsRunning)        return;
        if (string.IsNullOrEmpty(e.Text))    return;

        foreach (char c in e.Text)
            HandleCharacter(c.ToString());

        e.Handled = true;
    }

    /// Only non-printable keys that never appear in TextInput need handling here.
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (PageHost.Content != _typingPage) return;
        if (!_typingEngine.IsRunning)        return;

        if (e.Key == Key.Back)
        {
            HandleBackspace();
            e.Handled = true;
        }
        // NOTE: Do NOT add Key.Space — it double-fires on some layouts.
        //       Space arrives correctly through TextInputEvent.
    }

    // ── Round loading ────────────────────────────────────────────────────────
    private void LoadSubRound()
    {
        _countdownTimer?.Stop();
        _countdownTimer = null;
        _roundTimer?.Stop();
        _roundTimer     = null;
        _isCountingDown = false;
        _attemptsUsed   = 0;

        _typingEngine.ResetTimer();

        RoundDisplay     = $"{RoundName(_roundIndex)}  ·  {_subRoundIndex + 1} / 3";
        AttemptsLeft     = MaxAttemptsPerSubRound;
        CountdownValue   = 5;
        RemainingSeconds = TypingEngineService.TestDurationSeconds; // 60

        _typingEngine.SetTargetText(Paragraphs[_roundIndex][_subRoundIndex]);
        CurrentInput = string.Empty;

        _wpmPoints.Clear();
        _rawWpmPoints.Clear();
        _errorPoints.Clear();

        _typingEngine.Stop();

        StartCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        NotifyCountdownProperties();

        PageHost.Content = _typingPage;
        this.Focus();
    }

    // ── Start with 5-second countdown ────────────────────────────────────────
    private void StartSubRound()
    {
        if (_typingEngine.IsRunning || _isCountingDown) return;

        _isCountingDown  = true;
        _countdownValue  = 5;
        RemainingSeconds = 5;
        NotifyCountdownProperties();

        StartCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();

        // Play the first tick immediately so "5" has a sound
        _soundService?.PlayCountdownTick();

        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (_, _) =>
        {
            _countdownValue--;
            CountdownValue   = _countdownValue;
            RemainingSeconds = _countdownValue;

            if (_countdownValue <= 0)
            {
                _countdownTimer?.Stop();
                _countdownTimer = null;
                _isCountingDown = false;
                NotifyCountdownProperties();

                // "GO!" — brighter, longer tone signals round start
                _soundService?.PlayCountdownGo();

                // ── Begin the 60-second typing round ──
                _typingEngine.Start();
                RemainingSeconds = TypingEngineService.TestDurationSeconds; // 60

                _roundTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _roundTimer.Tick += (_, _) => UpdateStats();
                _roundTimer.Start();

                StartCommand.NotifyCanExecuteChanged();
                ResetCommand.NotifyCanExecuteChanged();
                this.Focus();
            }
            else
            {
                // Short tick for 4, 3, 2, 1
                _soundService?.PlayCountdownTick();
            }
        };
        _countdownTimer.Start();
        this.Focus();
    }

    // ── Reset ────────────────────────────────────────────────────────────────
    private void ResetSubRound()
    {
        if (!ResetCommand.CanExecute(null)) return;

        _attemptsUsed++;
        AttemptsLeft = MaxAttemptsPerSubRound - _attemptsUsed;

        _roundTimer?.Stop();
        _roundTimer = null;
        _typingEngine.Stop();
        _typingEngine.ResetTimer();
        _typingEngine.SetTargetText(_typingEngine.TargetText);

        CurrentInput = string.Empty;
        _wpmPoints.Clear();
        _rawWpmPoints.Clear();
        _errorPoints.Clear();

        StartCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();

        _soundService?.Play();
        this.Focus();
    }

    // ── Sub-round finished ───────────────────────────────────────────────────
    private void SubRoundFinished()
    {
        _roundTimer?.Stop();
        _roundTimer = null;
        _typingEngine.Stop();

        StartCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();

        var result = new RoundResult
        {
            RoundName      = RoundName(_roundIndex),
            SubRoundIndex  = _subRoundIndex,
            Wpm            = Wpm,
            Accuracy       = Accuracy,
            RawWpm         = _typingEngine.CalculateRawWpm(),
            CorrectChars   = _typingEngine.CountCorrectChars(_currentInput),
            IncorrectChars = _currentInput.TakeWhile((c, i) =>
                                 i < _typingEngine.TargetText.Length &&
                                 c != _typingEngine.TargetText[i]).Count(),
            MissedChars    = Math.Max(0, _typingEngine.TargetText.Length - _currentInput.Length),
            ExtraChars     = Math.Max(0, _currentInput.Length - _typingEngine.TargetText.Length),
            Consistency    = 100,
            AttemptsUsed   = _attemptsUsed,
            WpmPoints      = _wpmPoints.ToList(),
            RawWpmPoints   = _rawWpmPoints.ToList(),
            ErrorPoints    = _errorPoints.ToList()
        };

        _currentRoundSummary.SubRounds.Add(result);
        _ = DatabaseService.SaveRound(_playerName, result);

        bool isLastSubRound = _subRoundIndex == 2;
        bool isLastRound    = _roundIndex    == 2;

        _resultPage.SetResult(result, isLastRound: isLastSubRound && isLastRound);
        PageHost.Content = _resultPage;
    }

    // ── Navigation ───────────────────────────────────────────────────────────
    private void GoToNextSubRound()
    {
        if (_subRoundIndex < 2) { _subRoundIndex++; LoadSubRound(); }
        else                      ShowRoundSummary();
    }

    private void ShowRoundSummary()
    {
        _roundSummaries.Add(_currentRoundSummary);
        _roundSummaryPage.SetSummary(_currentRoundSummary, isLastRound: _roundIndex == 2);
        PageHost.Content = _roundSummaryPage;
    }

    private void GoToNextRound()
    {
        _roundIndex++;
        _subRoundIndex       = 0;
        _currentRoundSummary = new RoundSummary { RoundName = RoundName(_roundIndex) };
        LoadSubRound();
    }

    private void GoToSummary()
    {
        if (!_roundSummaries.Contains(_currentRoundSummary) &&
            _currentRoundSummary.SubRounds.Count > 0)
            _roundSummaries.Add(_currentRoundSummary);

        new SummaryWindow(_roundSummaries, _playerName).Show();
        Close();
    }

    private void ForceFinish() => GoToSummary();

    // ── Stats tick (every second during live round) ──────────────────────────
    private void UpdateStats()
    {
        if (!_typingEngine.IsRunning) return;

        Wpm              = _typingEngine.CalculateWpm(_currentInput);
        Accuracy         = _typingEngine.CalculateAccuracy(_currentInput);
        RemainingSeconds = _typingEngine.RemainingSeconds;

        double elapsed = _typingEngine.ElapsedSeconds;
        _wpmPoints.Add(new Point(elapsed, Wpm));
        _rawWpmPoints.Add(new Point(elapsed, _typingEngine.CalculateRawWpm()));
        _errorPoints.Add(new Point(elapsed, 100 - Accuracy));

        if (RemainingSeconds <= 0) SubRoundFinished();
    }

    // ── Formatted text builder ───────────────────────────────────────────────
    private void UpdateFormattedText()
    {
        var target = _typingEngine.TargetText;
        var input  = _currentInput;
        var list   = new List<FormattedChar>();

        for (int i = 0; i < target.Length; i++)
        {
            string fg = "#808080";
            if (i < input.Length)       fg = input[i] == target[i] ? "#4CAF50" : "#F44336";
            else if (i == input.Length) fg = "#FFFFFF";

            list.Add(new FormattedChar
            {
                Character  = target[i].ToString(),
                Foreground = fg,
                IsCaret    = i == input.Length
            });
        }

        if (input.Length > target.Length)
            for (int i = target.Length; i < input.Length; i++)
                list.Add(new FormattedChar
                {
                    Character  = input[i].ToString(),
                    Foreground = "#FF9800",
                    IsCaret    = i == input.Length - 1
                });

        FormattedText = new ObservableCollection<FormattedChar>(list);
    }

    // ── Input helpers ────────────────────────────────────────────────────────
    public void HandleCharacter(string text)
    {
        if (!_typingEngine.IsRunning) return;
        _typingEngine.RegisterKeystroke();
        _soundService?.Play();
        CurrentInput += text[0];
    }

    public void HandleBackspace()
    {
        if (!_typingEngine.IsRunning || _currentInput.Length == 0) return;
        _soundService?.Play();
        CurrentInput = _currentInput[..^1];
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static string RoundName(int index) => index switch
    {
        0 => "Easy", 1 => "Medium", 2 => "Hard", _ => "Unknown"
    };

    private void NotifyCountdownProperties()
    {
        OnPropertyChanged(nameof(IsCountingDown));
        OnPropertyChanged(nameof(CountdownValue));
        OnPropertyChanged(nameof(CountdownLabel));
        OnPropertyChanged(nameof(CountdownOverlayVisible));
    }

    // ── Bindable properties ──────────────────────────────────────────────────
    public string RoundDisplay
    {
        get => _roundDisplay;
        set { _roundDisplay = value; OnPropertyChanged(); }
    }

    public int AttemptsLeft
    {
        get => _attemptsLeft;
        set { _attemptsLeft = value; OnPropertyChanged(); }
    }

    public double Wpm
    {
        get => _wpm;
        set { _wpm = value; OnPropertyChanged(); }
    }

    public double Accuracy
    {
        get => _accuracy;
        set { _accuracy = value; OnPropertyChanged(); }
    }

    public int RemainingSeconds
    {
        get => _remainingSeconds;
        set { _remainingSeconds = value; OnPropertyChanged(); }
    }

    public bool IsCountingDown
    {
        get => _isCountingDown;
        set { _isCountingDown = value; NotifyCountdownProperties(); }
    }

    public int CountdownValue
    {
        get => _countdownValue;
        set { _countdownValue = value; OnPropertyChanged(); }
    }

    public string CountdownLabel          => _isCountingDown ? "GET READY" : "SEC";
    public bool   CountdownOverlayVisible => _isCountingDown;

    public ObservableCollection<FormattedChar> FormattedText
    {
        get => _formattedText;
        set { _formattedText = value; OnPropertyChanged(); }
    }

    public string CurrentInput
    {
        get => _currentInput;
        set
        {
            _currentInput = value;
            UpdateFormattedText();
            if (_typingEngine.IsRunning)
            {
                Wpm      = _typingEngine.CalculateWpm(_currentInput);
                Accuracy = _typingEngine.CalculateAccuracy(_currentInput);
            }
            OnPropertyChanged();
        }
    }
}