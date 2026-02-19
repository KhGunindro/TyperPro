using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using TyperPro.Models;

namespace TyperPro.Views;

public partial class RoundSummaryPage : UserControl
{
    public event Action? OnNextRound;
    public event Action? OnFinish;

    private static readonly string[] SubColors = { "#4CAF50", "#FF9800", "#2196F3" };

    public RoundSummaryPage()
    {
        InitializeComponent();
    }

    public void SetSummary(RoundSummary summary, bool isLastRound)
    {
        // Header
        RoundBadge.Text  = "ROUND COMPLETE";
        RoundTitle.Text  = summary.RoundName.ToUpper();
        NextBtnText.Text = isLastRound ? "VIEW SUMMARY" : "NEXT ROUND";
        NextBtn.IsVisible = true;

        // Averages
        AvgWpmText.Text  = $"{summary.AvgWpm:F0}";
        AvgRawText.Text  = $"{summary.AvgRawWpm:F0}";
        AvgAccText.Text  = $"{summary.AvgAccuracy:F0}";
        BestWpmText.Text = $"{summary.BestWpm:F0}";

        // Sub-round cards
        SubRoundGrid.Children.Clear();
        for (int i = 0; i < summary.SubRounds.Count; i++)
        {
            var r   = summary.SubRounds[i];
            var col = i;
            var card = BuildSubCard(r, i);
            Grid.SetColumn(card, col);
            SubRoundGrid.Children.Add(card);
        }

        // Trigger header fade-in
        HeaderPanel.Opacity = 1;

        // Draw chart after layout
        bool drawn = false;
        LayoutUpdated += OnLayout;

        void OnLayout(object? s, EventArgs e)
        {
            if (drawn) return;
            if (WpmChart.Bounds.Width < 10) return;
            drawn = true;
            LayoutUpdated -= OnLayout;
            DrawOverlaidWpmChart(summary.SubRounds);
        }
    }

    private static Border BuildSubCard(RoundResult r, int index)
    {
        var card = new Border
        {
            Classes     = { "subCard" },
            BorderBrush = SolidColorBrush.Parse(SubColors[index % SubColors.Length]),
            Margin      = new Thickness(0, 0, index < 2 ? 8 : 0, 0)
        };

        var stack = new StackPanel { Spacing = 0 };

        stack.Children.Add(new TextBlock
        {
            Text          = $"SUB-ROUND {index + 1}",
            FontSize      = 10,
            FontWeight    = Avalonia.Media.FontWeight.Bold,
            Foreground    = SolidColorBrush.Parse(SubColors[index % SubColors.Length]),
            LetterSpacing = 3,
            Margin        = new Thickness(0, 0, 0, 12),
            FontFamily    = new FontFamily("Courier New")
        });

        void AddStat(string label, string value, string color)
        {
            var g = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Margin = new Thickness(0, 4, 0, 0)
            };
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
            stack.Children.Add(g);
        }

        AddStat("WPM",       $"{r.Wpm:F0}",       "#E8E8E8");
        AddStat("RAW",       $"{r.RawWpm:F0}",     "#666666");
        AddStat("ACC",       $"{r.Accuracy:F0}%",  "#E8E8E8");
        AddStat("CORRECT",   r.CorrectChars.ToString(),    "#4CAF50");
        AddStat("INCORRECT", r.IncorrectChars.ToString(),  "#F44336");
        AddStat("RESETS",    r.AttemptsUsed.ToString(),    "#888888");

        card.Child = stack;
        return card;
    }

    // ── Chart ─────────────────────────────────────────────────────────────────

    private void DrawOverlaidWpmChart(List<RoundResult> subRounds)
    {
        WpmChart.Children.Clear();
        double w = WpmChart.Bounds.Width;
        double h = WpmChart.Bounds.Height;
        if (w < 2 || h < 2) return;

        var allPts = subRounds.SelectMany(r => r.WpmPoints).ToList();
        if (allPts.Count == 0) return;

        double maxX = allPts.Max(p => p.X); if (maxX <= 0) maxX = 1;
        double maxY = allPts.Max(p => p.Y); if (maxY <= 0) maxY = 1;
        maxY *= 1.15;

        // Grid
        for (int i = 0; i <= 4; i++)
        {
            double y = h - h * i / 4;
            WpmChart.Children.Add(new Line
            {
                StartPoint = new Point(0, y), EndPoint = new Point(w, y),
                Stroke = new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1
            });
        }

        // One line per sub-round
        for (int s = 0; s < subRounds.Count; s++)
        {
            var pts = subRounds[s].WpmPoints;
            if (pts.Count < 2) continue;

            var color = Color.Parse(SubColors[s % SubColors.Length]);
            var geo   = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                bool first = true;
                foreach (var p in pts)
                {
                    double px = p.X / maxX * w;
                    double py = h - p.Y / maxY * h;
                    if (first) { ctx.BeginFigure(new Point(px, py), false); first = false; }
                    else ctx.LineTo(new Point(px, py));
                }
            }
            WpmChart.Children.Add(new Avalonia.Controls.Shapes.Path
            {
                Data            = geo,
                Stroke          = new SolidColorBrush(color),
                StrokeThickness = 2,
                StrokeLineCap   = PenLineCap.Round,
                StrokeJoin      = PenLineJoin.Round
            });
        }
    }

    private void Next_Click(object? sender, RoutedEventArgs e)   => OnNextRound?.Invoke();
    private void Finish_Click(object? sender, RoutedEventArgs e) => OnFinish?.Invoke();
}