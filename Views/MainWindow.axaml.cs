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
using System.Threading.Tasks;
using TyperPro.Models;
using TyperPro.Services;

namespace TyperPro.Views;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly TypingEngineService _typingEngine;
    private TypingSoundService?          _soundService;
    private string                       _playerName = string.Empty;

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

    private readonly NameInputPage    _nameInputPage;
    private readonly TypingPage       _typingPage;
    private readonly ResultPage       _resultPage;
    private readonly RoundSummaryPage _roundSummaryPage;

    public IRelayCommand StartCommand { get; }
    public IRelayCommand ResetCommand { get; }

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
    
    private static readonly string[][] Paragraphs =
    {
        // --- EASY: Long, flowing narratives with simple vocabulary and standard rhythm ---
        new[] {
            "The old lighthouse stood tall on the jagged cliffs, watching over the cold blue sea for over a hundred years. Every night, the giant glass lens turned slowly, sending a bright beam of light far out into the dark waves to guide the ships safely home to the harbor. The keeper of the light was a kind man named Samuel, who spent his days painting the wooden stairs and cleaning the glass until it sparkled like a diamond in the sun. He lived a quiet life with his small dog and a garden full of red roses that grew against the white stone walls. Even when the great storms came and the wind howled around the tower, Samuel felt safe and warm inside his little kitchen with a cup of hot tea. He knew that his work was important to the sailors who were far from land, and he took great pride in making sure the light never went out, no matter how hard the rain fell or how loud the thunder rolled across the sky.",
            "A winding river flowed through the center of the deep green valley, bringing life to the many animals that lived among the tall grass and shady trees. In the spring, the water was high and fast from the melting snow on the mountains, but by the middle of summer, it became a gentle stream where deer came to drink in the early morning light. A group of friends often brought their wooden canoe to the water’s edge, pushing off into the current to spend the whole day exploring the hidden bends and quiet pools of the river. They brought a basket filled with sandwiches, apples, and cold juice to eat under the shade of a large willow tree that dipped its long branches into the water. As the sun began to set, they would paddle back to their campsite, tired but happy, to build a small fire and roast marshmallows under a sky filled with thousands of bright stars.",
            "Walking through the city park in the middle of autumn is like stepping into a painting filled with gold, red, and bright yellow leaves. The air is crisp and cool, making people wrap their scarves a little tighter as they walk along the stone paths that lead past the big fountain in the square. Squirrels run quickly across the grass, gathering nuts to hide for the long winter months ahead, while children jump into large piles of leaves that their parents have raked together. On the wooden benches, older couples sit together and talk quietly, enjoying the last bit of warmth from the afternoon sun before the evening chill sets in. There is a sense of change in the air, a feeling that the earth is getting ready to rest, and yet there is so much beauty in this final burst of color before the first white snowflakes begin to fall from the grey clouds above."
        },

        // --- MEDIUM: Longer sentences, socio-technical themes, and mid-range punctuation ---
        new[] {
            "The transition toward renewable energy sources represents a monumental shift in global infrastructure that requires both political will and massive private investment. While solar and wind power have become increasingly cost-effective over the last decade, the challenge of energy storage remains a significant hurdle for engineers to overcome. Without high-capacity battery systems or innovative grid management, the intermittent nature of these green technologies could lead to instability in the power supply during peak hours of consumption. Furthermore, the geopolitical landscape is being reshaped as nations scramble to secure the rare earth minerals necessary for manufacturing high-tech components. We must also consider the workforce transition, ensuring that those currently employed in the fossil fuel industry are provided with the education and resources needed to pivot into the emerging green economy. It is a race against time that demands cooperation on an international scale to mitigate the most severe effects of a changing climate.",
            "Urban planning in the twenty-first century has evolved to prioritize the 'walkable city' model, which seeks to reduce our heavy reliance on personal automobiles in favor of public transit and cycling. By designing neighborhoods where essential services—such as grocery stores, schools, and healthcare facilities—are within a fifteen-minute walk, planners can significantly improve the quality of life for residents. This approach not only reduces traffic congestion and air pollution but also fosters a stronger sense of community as people interact more frequently in shared public spaces. However, implementing these changes in established metropolitan areas often faces resistance from those who fear the loss of parking or the gentrification of older districts. Successful transformation requires a delicate balance of architectural innovation, social equity, and transparent governance to ensure that the benefits of modernization are shared by all members of society, regardless of their income level.",
            "The advent of the internet has fundamentally altered the way human beings process information, shifting our focus from deep, sustained reading to a more fragmented and rapid style of scanning content. Cognitive scientists are currently investigating how this constant exposure to hyperlinks and digital notifications affects our ability to concentrate on complex tasks for extended periods. While we now have access to the sum of human knowledge at our fingertips, there is a growing concern that our analytical skills may be eroding in favor of superficial understanding. This digital shift has also impacted the publishing industry, as traditional newspapers struggle to compete with the viral nature of social media algorithms that prioritize engagement over factual accuracy. To thrive in this new environment, individuals must practice 'digital mindfulness,' consciously choosing when to disconnect from the screen and engage in the slow, deliberate thought processes that lead to true wisdom and long-term memory retention."
        },

        // --- HARD: Extreme length, dense scientific/philosophical jargon, and heavy punctuation ---
        new[] {
            "The architectural paradigm of the 'panopticon'—as conceptualized by Jeremy Bentham and later critiqued by Michel Foucault—serves as a chilling metaphor for the ubiquitous surveillance state of the digital epoch. In this theoretical framework, the mere possibility of being observed functions as a mechanism of social control, compelling individuals to internalize the gaze of authority and regulate their own behavior accordingly. When transposed onto contemporary data-harvesting practices, this phenomenon manifests as the 'algorithmic panopticon,' where predictive analytics and biometric metadata coalesce to form a comprehensive profile of the private citizen. The ethical ramifications are staggering: if our preferences, movements, and even our subconscious biases are quantifiable, the traditional notion of 'free will' becomes a fragile abstraction. We find ourselves ensnared in a web of recursive feedback loops, where our past digital footprints dictate our future opportunities in a manner that is often opaque, unaccountable, and fundamentally antithetical to the principles of an open society.",
            "Astrophysical observations regarding the accelerated expansion of the universe have necessitated the postulation of 'dark energy'—a hypothetical form of energy that permeates all of space and exerts a negative, repulsive pressure. This cosmological constant (represented by the Greek letter Lambda) remains the most widely accepted explanation within the Lambda-CDM model; however, the discrepancy between the observed vacuum energy density and the theoretical predictions of quantum field theory is often described as the 'worst theoretical prediction in the history of physics.' Scientists are currently utilizing massive subterranean detectors and space-based interferometers to probe the fundamental nature of this elusive force, hoping to discern whether it is a static property of space-time or a dynamic field (such as quintessence) that evolves over eons. The resolution of this enigma may require a radical synthesis of General Relativity and Quantum Mechanics—two frameworks that remain stubbornly incompatible at the Planck scale—potentially leading to a paradigm shift that redefines our comprehension of the 'Big Freeze' and the ultimate fate of the cosmos.",
            "The ontological status of 'qualia'—the subjective, first-person experiences of sensory perception, such as the specific redness of a rose or the agonizing sting of a burn—represents the 'hard problem' of consciousness that continues to baffle neurophilosophers. Materialist reductionists posit that these experiences are merely epiphenomena of complex neural firing patterns within the cerebral cortex; yet, this fails to explain *why* such physical processes should be accompanied by an internal felt-sense at all. Frank Jackson’s famous 'Mary’s Room' thought experiment suggests that even if one possessed exhaustive physical knowledge of the world, the actual experience of color would provide a new, non-physical fact, thereby refuting strict physicalism. This leads some to explore panpsychism—the radical idea that consciousness is a fundamental, ubiquitous feature of the universe, akin to mass or charge—rather than an emergent property of biological evolution. Navigating this labyrinthine discourse requires a rigorous deconstruction of our most basic assumptions regarding the mind-body dualism that has haunted Western metaphysics since the Cartesian revolution."
        }
    };

    public MainWindow()
    {
        _typingEngine = new TypingEngineService();

        StartCommand = new RelayCommand(StartSubRound, () => !_typingEngine.IsRunning && !_isCountingDown);
        ResetCommand = new RelayCommand(ResetSubRound, () => _typingEngine.IsRunning && _attemptsUsed < MaxAttemptsPerSubRound);

        InitializeComponent();

        this.AddHandler(InputElement.TextInputEvent, OnWindowTextInput, RoutingStrategies.Tunnel);
        this.AddHandler(InputElement.KeyDownEvent,   OnWindowKeyDown,   RoutingStrategies.Tunnel);
        this.PointerPressed += (_, _) => Focus();

        // Sound init
        Task.Run(() =>
        {
            try   { _soundService = new TypingSoundService(); }
            catch (Exception ex)
            {
                Console.WriteLine($"[Audio] Sound disabled: {ex.Message}");
                _soundService = null;
            }
        });

        _nameInputPage = new NameInputPage();
        _nameInputPage.OnNameSubmitted += OnNameSubmitted;

        _typingPage             = new TypingPage();
        _typingPage.DataContext = this;

        _resultPage              = new ResultPage();
        _resultPage.OnNextRound += GoToNextSubRound;
        _resultPage.OnClose     += ForceFinish;

        _roundSummaryPage              = new RoundSummaryPage();
        _roundSummaryPage.OnNextRound += GoToNextRound;
        _roundSummaryPage.OnFinish    += GoToSummary;

        // Start on name input page
        PageHost.Content = _nameInputPage;

        Dispatcher.UIThread.Post(() =>
        {
            Focus();
            _nameInputPage.FocusInput();
        }, DispatcherPriority.Loaded);
    }

    private void OnNameSubmitted(string name)
    {
        _playerName          = name;
        _currentRoundSummary = new RoundSummary { RoundName = RoundName(_roundIndex) };
        LoadSubRound();
    }

    // ── Input handlers ───────────────────────────────────────────────────────
    private void OnWindowTextInput(object? sender, TextInputEventArgs e)
    {
        if (PageHost.Content != _typingPage) return;
        if (!_typingEngine.IsRunning)        return;
        if (string.IsNullOrEmpty(e.Text))    return;

        foreach (char c in e.Text)
            HandleCharacter(c.ToString());

        e.Handled = true;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (PageHost.Content != _typingPage) return;
        if (!_typingEngine.IsRunning)        return;

        if (e.Key == Key.Back)
        {
            HandleBackspace();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            SubRoundFinished();
            e.Handled = true;
        }
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
        RemainingSeconds = TypingEngineService.TestDurationSeconds;

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
        Focus();
    }

    // ── Start with countdown ─────────────────────────────────────────────────
    private void StartSubRound()
    {
        if (_typingEngine.IsRunning || _isCountingDown) return;

        _isCountingDown  = true;
        _countdownValue  = 5;
        RemainingSeconds = 5;
        NotifyCountdownProperties();

        StartCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();

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

                _soundService?.PlayCountdownGo();

                _typingEngine.Start();
                RemainingSeconds = TypingEngineService.TestDurationSeconds;

                _roundTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _roundTimer.Tick += (_, _) => UpdateStats();
                _roundTimer.Start();

                StartCommand.NotifyCanExecuteChanged();
                ResetCommand.NotifyCanExecuteChanged();
                Focus();
            }
            else
            {
                _soundService?.PlayCountdownTick();
            }
        };
        _countdownTimer.Start();
        Focus();
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
        Focus();
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

        _ = Task.Run(async () =>
        {
            try   { await DatabaseService.SaveRound(_playerName, result); }
            catch (Exception ex) { Console.WriteLine($"[DB] Save failed: {ex.Message}"); }
        });

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

    // ── Stats tick ───────────────────────────────────────────────────────────
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

    // ── Formatted text ───────────────────────────────────────────────────────
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

    // ── Properties ───────────────────────────────────────────────────────────
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