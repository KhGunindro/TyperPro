using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using TyperPro.Models;

namespace TyperPro.Views;

public partial class ResultPage : UserControl
{
    public event Action? OnNextRound;
    public event Action? OnClose;

    public ResultPage()
    {
        InitializeComponent();
    }

    public void SetResult(RoundResult result, bool isLastRound)
    {
        // Header
        RoundBadge.Text    = "ROUND COMPLETE";
        RoundNameText.Text = result.RoundName.ToUpper();
        AttemptsText.Text  = $"ATTEMPT {result.AttemptsUsed + 1} / 3";

        // Primary stats
        WpmText.Text   = $"{result.Wpm:F0}";
        RawText.Text   = $"{result.RawWpm:F0}";
        AccText.Text   = $"{result.Accuracy:F0}";
        ConsText.Text  = $"{result.Consistency:F0}";

        // Character stats
        CorrectText.Text      = result.CorrectChars.ToString();
        IncorrectText.Text    = result.IncorrectChars.ToString();
        MissedText.Text       = result.MissedChars.ToString();
        ExtraText.Text        = result.ExtraChars.ToString();
        AttemptsCountText.Text = result.AttemptsUsed.ToString();

        NextRoundBtn.IsVisible = !isLastRound;

        // Draw charts after layout (use LayoutUpdated to ensure canvas has size)
        bool drawn = false;
        LayoutUpdated += OnLayout;

        void OnLayout(object? s, EventArgs e)
        {
            if (drawn) return;
            if (WpmChart.Bounds.Width < 10) return;
            drawn = true;
            LayoutUpdated -= OnLayout;
            DrawWpmChart(result.WpmPoints, result.RawWpmPoints);
            DrawErrorChart(result.ErrorPoints);
            DrawCharBar(result);
        }
    }

    // ── WPM chart ────────────────────────────────────────────────────────────

    private void DrawWpmChart(List<Point> wpmPts, List<Point> rawPts)
    {
        WpmChart.Children.Clear();
        double w = WpmChart.Bounds.Width;
        double h = WpmChart.Bounds.Height;
        if (w < 2 || h < 2) return;

        var all = wpmPts.Concat(rawPts).ToList();
        if (all.Count == 0) { DrawEmptyState(WpmChart, w, h); return; }

        double maxX = all.Max(p => p.X); if (maxX <= 0) maxX = 1;
        double maxY = all.Max(p => p.Y); if (maxY <= 0) maxY = 1;
        maxY *= 1.15; // headroom

        DrawGrid(WpmChart, w, h, maxY);

        DrawLine(WpmChart, rawPts, w, h, maxX, maxY, Color.FromRgb(0x33, 0x33, 0x33), 1.5);
        DrawLine(WpmChart, wpmPts, w, h, maxX, maxY, Color.FromRgb(0x4C, 0xAF, 0x50), 2);
        DrawDots(WpmChart, wpmPts, w, h, maxX, maxY, Color.FromRgb(0x4C, 0xAF, 0x50));
    }

    // ── Error chart ──────────────────────────────────────────────────────────

    private void DrawErrorChart(List<Point> errorPts)
    {
        ErrorChart.Children.Clear();
        double w = ErrorChart.Bounds.Width;
        double h = ErrorChart.Bounds.Height;
        if (w < 2 || h < 2) return;

        if (errorPts.Count == 0) { DrawEmptyState(ErrorChart, w, h); return; }

        double maxX = errorPts.Max(p => p.X); if (maxX <= 0) maxX = 1;
        double maxY = errorPts.Max(p => p.Y); if (maxY <= 0) maxY = 10;
        maxY = Math.Max(maxY * 1.15, 10);

        DrawGrid(ErrorChart, w, h, maxY);
        DrawFill(ErrorChart, errorPts, w, h, maxX, maxY, Color.FromArgb(0x22, 0xF4, 0x43, 0x36));
        DrawLine(ErrorChart, errorPts, w, h, maxX, maxY, Color.FromRgb(0xF4, 0x43, 0x36), 2);
        DrawDots(ErrorChart, errorPts, w, h, maxX, maxY, Color.FromRgb(0xF4, 0x43, 0x36));
    }

    // ── Character bar ────────────────────────────────────────────────────────

    private void DrawCharBar(RoundResult result)
    {
        CharBar.Children.Clear();
        double w = CharBar.Bounds.Width;
        if (w < 2) return;

        int total = result.CorrectChars + result.IncorrectChars + result.MissedChars + result.ExtraChars;
        if (total == 0) return;

        var segments = new[]
        {
            (result.CorrectChars,   Color.FromRgb(0x4C, 0xAF, 0x50)),
            (result.IncorrectChars, Color.FromRgb(0xF4, 0x43, 0x36)),
            (result.MissedChars,    Color.FromRgb(0xFF, 0x98, 0x00)),
            (result.ExtraChars,     Color.FromRgb(0xFF, 0x70, 0x00)),
        };

        double x = 0;
        foreach (var (count, color) in segments)
        {
            if (count <= 0) continue;
            double segW = w * count / total;
            var rect = new Rectangle
            {
                Width  = Math.Max(segW - 1, 0),
                Height = 6,
                Fill   = new SolidColorBrush(color),
                RadiusX = 2,
                RadiusY = 2
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, 0);
            CharBar.Children.Add(rect);
            x += segW;
        }
    }

    // ── Shared drawing helpers ────────────────────────────────────────────────

    private static void DrawGrid(Canvas canvas, double w, double h, double maxY)
    {
        int lines = 4;
        for (int i = 0; i <= lines; i++)
        {
            double y = h - (h * i / lines);
            var line = new Line
            {
                StartPoint = new Point(0, y),
                EndPoint   = new Point(w, y),
                Stroke     = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)),
                StrokeThickness = 1
            };
            canvas.Children.Add(line);

            // Y-axis label
            double val = maxY * i / lines;
            var lbl = new TextBlock
            {
                Text       = $"{val:F0}",
                FontSize   = 8,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
                FontFamily = new FontFamily("Courier New")
            };
            Canvas.SetLeft(lbl, 2);
            Canvas.SetTop(lbl, y - 10);
            canvas.Children.Add(lbl);
        }
    }

    private static void DrawLine(Canvas canvas, List<Point> pts, double w, double h,
                                  double maxX, double maxY, Color color, double thickness)
    {
        if (pts.Count < 2) return;
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            bool first = true;
            foreach (var p in pts)
            {
                double px = p.X / maxX * w;
                double py = h - (p.Y / maxY * h);
                if (first) { ctx.BeginFigure(new Point(px, py), false); first = false; }
                else ctx.LineTo(new Point(px, py));
            }
        }
        canvas.Children.Add(new Avalonia.Controls.Shapes.Path
        {
            Data            = geo,
            Stroke          = new SolidColorBrush(color),
            StrokeThickness = thickness,
            StrokeLineCap   = PenLineCap.Round,
            StrokeJoin      = PenLineJoin.Round
        });
    }

    private static void DrawFill(Canvas canvas, List<Point> pts, double w, double h,
                                  double maxX, double maxY, Color fillColor)
    {
        if (pts.Count < 2) return;
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            double firstX = pts[0].X / maxX * w;
            ctx.BeginFigure(new Point(firstX, h), true);
            foreach (var p in pts)
            {
                double px = p.X / maxX * w;
                double py = h - (p.Y / maxY * h);
                ctx.LineTo(new Point(px, py));
            }
            double lastX = pts[^1].X / maxX * w;
            ctx.LineTo(new Point(lastX, h));
        }
        canvas.Children.Add(new Avalonia.Controls.Shapes.Path
        {
            Data = geo,
            Fill = new SolidColorBrush(fillColor)
        });
    }

    private static void DrawDots(Canvas canvas, List<Point> pts, double w, double h,
                                  double maxX, double maxY, Color color)
    {
        foreach (var p in pts)
        {
            double px = p.X / maxX * w;
            double py = h - (p.Y / maxY * h);
            var dot = new Ellipse
            {
                Width  = 4,
                Height = 4,
                Fill   = new SolidColorBrush(color)
            };
            Canvas.SetLeft(dot, px - 2);
            Canvas.SetTop(dot, py - 2);
            canvas.Children.Add(dot);
        }
    }

    private static void DrawEmptyState(Canvas canvas, double w, double h)
    {
        var lbl = new TextBlock
        {
            Text       = "no data",
            FontSize   = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            FontFamily = new FontFamily("Courier New")
        };
        Canvas.SetLeft(lbl, w / 2 - 20);
        Canvas.SetTop(lbl, h / 2 - 6);
        canvas.Children.Add(lbl);
    }

    // ── Button handlers ──────────────────────────────────────────────────────

    private void NextRound_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => OnNextRound?.Invoke();

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => OnClose?.Invoke();
}