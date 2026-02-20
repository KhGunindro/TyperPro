using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using TyperPro.Models;
using TyperPro.ViewModels;

namespace TyperPro.Views;

public partial class SummaryWindow : Window
{
    private SummaryViewModel _vm = null!;

    // ── FIX: Required by Avalonia XAML loader ────────────────────────────────
    public SummaryWindow() : this(new List<RoundSummary>(), "Player") { }

    public SummaryWindow(List<RoundSummary> summaries, string playerName)
    {
        InitializeComponent();
        _vm = new SummaryViewModel(summaries, playerName);
        DataContext = _vm;
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        PlayerNameText.Text = _vm.PlayerName;
        TaglineText.Text    = BuildTagline(_vm.OverallAvgWpm, _vm.OverallAvgAccuracy);
        HeroPanel.Opacity   = 1;

        AvgWpmText.Text  = $"{_vm.OverallAvgWpm:F0}";
        AvgRawText.Text  = $"{_vm.OverallAvgRaw:F0}";
        AvgAccText.Text  = $"{_vm.OverallAvgAccuracy:F0}";
        BestWpmText.Text = $"{_vm.OverallBestWpm:F0}";

        BuildRoundCards(_vm.RoundSummaries.ToList());

        bool drawn = false;
        LayoutUpdated += OnLayout;
        void OnLayout(object? s, EventArgs ev)
        {
            if (drawn) return;
            if (WpmGraphCanvas.Bounds.Width < 10) return;
            drawn = true;
            LayoutUpdated -= OnLayout;
            DrawChart(WpmGraphCanvas,   _vm.AverageWpmPoints,   Color.FromRgb(0x4C, 0xAF, 0x50), withFill: true);
            DrawChart(ErrorGraphCanvas, _vm.AverageErrorPoints, Color.FromRgb(0xF4, 0x43, 0x36), withFill: true);
        }
    }

    private void BuildRoundCards(List<RoundSummary> summaries)
    {
        var roundColors = new[] { "#4CAF50", "#FF9800", "#F44336" };
        var subColors   = new[] { "#4CAF50", "#FF9800", "#2196F3" };

        for (int ri = 0; ri < summaries.Count && ri < 3; ri++)
        {
            var summary = summaries[ri];

            var card = new Border
            {
                Background      = new SolidColorBrush(Color.Parse("#0A0A0A")),
                BorderBrush     = SolidColorBrush.Parse(roundColors[ri % roundColors.Length]),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding         = new Thickness(20, 16),
                Margin          = new Thickness(0, 0, ri < 2 ? 8 : 0, 0)
            };

            var stack = new StackPanel { Spacing = 0 };

            stack.Children.Add(new TextBlock
            {
                Text          = summary.RoundName.ToUpper(),
                FontSize      = 10,
                FontWeight    = Avalonia.Media.FontWeight.Bold,
                Foreground    = SolidColorBrush.Parse(roundColors[ri % roundColors.Length]),
                LetterSpacing = 3,
                Margin        = new Thickness(0, 0, 0, 12),
                FontFamily    = new FontFamily("Courier New")
            });

            AddStat(stack, "AVG WPM", $"{summary.AvgWpm:F0}",      "#E8E8E8");
            AddStat(stack, "AVG RAW", $"{summary.AvgRawWpm:F0}",   "#666666");
            AddStat(stack, "AVG ACC", $"{summary.AvgAccuracy:F0}%","#E8E8E8");
            AddStat(stack, "BEST",    $"{summary.BestWpm:F0} wpm", "#4CAF50");

            stack.Children.Add(new Border
            {
                Height     = 1,
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                Margin     = new Thickness(0, 12, 0, 12)
            });

            for (int si = 0; si < summary.SubRounds.Count; si++)
            {
                var r = summary.SubRounds[si];
                stack.Children.Add(new TextBlock
                {
                    Text       = $"  {si + 1}  ·  {r.Wpm:F0} wpm  ·  {r.Accuracy:F0}%",
                    FontSize   = 10,
                    Foreground = SolidColorBrush.Parse(subColors[si % subColors.Length]),
                    FontFamily = new FontFamily("Courier New"),
                    Margin     = new Thickness(0, 2, 0, 2)
                });
            }

            card.Child = stack;
            Grid.SetColumn(card, ri);
            RoundCardsGrid.Children.Add(card);
        }
    }

    private static void AddStat(StackPanel parent, string label, string value, string color)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 4, 0, 0) };
        g.Children.Add(new TextBlock
        {
            Text              = label,
            FontSize          = 9,
            Foreground        = SolidColorBrush.Parse("#333333"),
            FontFamily        = new FontFamily("Courier New"),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        var val = new TextBlock
        {
            Text       = value,
            FontSize   = 16,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = SolidColorBrush.Parse(color),
            FontFamily = new FontFamily("Courier New")
        };
        Grid.SetColumn(val, 1);
        g.Children.Add(val);
        parent.Children.Add(g);
    }

    private static void DrawChart(Canvas canvas, List<Point> pts, Color lineColor, bool withFill)
    {
        canvas.Children.Clear();
        double w = canvas.Bounds.Width, h = canvas.Bounds.Height;
        if (w < 2 || h < 2 || pts.Count == 0) return;

        double maxX = pts.Max(p => p.X); if (maxX <= 0) maxX = 1;
        double maxY = pts.Max(p => p.Y); if (maxY <= 0) maxY = 1;
        maxY *= 1.15;

        for (int i = 0; i <= 4; i++)
        {
            double y = h - h * i / 4;
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(0, y), EndPoint = new Point(w, y),
                Stroke = new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1
            });
            canvas.Children.Add(new TextBlock
            {
                Text = $"{maxY * i / 4 / 1.15:F0}", FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(0x28, 0x28, 0x28)),
                FontFamily = new FontFamily("Courier New"),
                [Canvas.LeftProperty] = 2.0, [Canvas.TopProperty] = y - 10
            });
        }

        if (withFill && pts.Count >= 2)
        {
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(new Point(pts[0].X / maxX * w, h), true);
                foreach (var p in pts) ctx.LineTo(new Point(p.X / maxX * w, h - p.Y / maxY * h));
                ctx.LineTo(new Point(pts[^1].X / maxX * w, h));
            }
            canvas.Children.Add(new Avalonia.Controls.Shapes.Path
            {
                Data = geo,
                Fill = new SolidColorBrush(Color.FromArgb(0x18, lineColor.R, lineColor.G, lineColor.B))
            });
        }

        if (pts.Count >= 2)
        {
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                bool first = true;
                foreach (var p in pts)
                {
                    double px = p.X / maxX * w, py = h - p.Y / maxY * h;
                    if (first) { ctx.BeginFigure(new Point(px, py), false); first = false; }
                    else ctx.LineTo(new Point(px, py));
                }
            }
            canvas.Children.Add(new Avalonia.Controls.Shapes.Path
            {
                Data = geo, Stroke = new SolidColorBrush(lineColor),
                StrokeThickness = 2, StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round
            });
        }

        foreach (var p in pts)
        {
            double px = p.X / maxX * w, py = h - p.Y / maxY * h;
            var dot = new Ellipse { Width = 4, Height = 4, Fill = new SolidColorBrush(lineColor) };
            Canvas.SetLeft(dot, px - 2); Canvas.SetTop(dot, py - 2);
            canvas.Children.Add(dot);
        }
    }

    private static string BuildTagline(double avgWpm, double avgAcc) => avgWpm switch
    {
        >= 100 => $"{avgWpm:F0} wpm average — you are genuinely fast.",
        >= 70  => $"{avgWpm:F0} wpm average — solid session, keep pushing.",
        >= 50  => $"{avgWpm:F0} wpm average — good effort, room to grow.",
        _      => $"{avgWpm:F0} wpm average — every session counts."
    };

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();
}