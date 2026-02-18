using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Linq;
using TyperPro.ViewModels;

namespace TyperPro.Views;

public partial class ResultDialog : Window
{
    public ResultDialog()
    {
        InitializeComponent();
        Loaded += (s, e) => DrawGraph();   // Draw when window is fully loaded
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DrawGraph()
    {
        if (DataContext is not ResultViewModel vm) return;

        var canvas = this.FindControl<Canvas>("GraphCanvas");
        if (canvas == null) return;

        canvas.Children.Clear();

        double width = canvas.Bounds.Width;
        double height = canvas.Bounds.Height;
        if (width <= 0 || height <= 0) return;

        // Draw axes
        canvas.Children.Add(new Line
        {
            StartPoint = new Point(0, height),
            EndPoint = new Point(width, height),
            Stroke = Brushes.Gray,
            StrokeThickness = 1
        });
        canvas.Children.Add(new Line
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, height),
            Stroke = Brushes.Gray,
            StrokeThickness = 1
        });

        void DrawPolyline(Point[] points, IBrush color)
        {
            if (points.Length < 2) return;
            var polyline = new Polyline
            {
                Points = new Avalonia.Collections.AvaloniaList<Point>(
                    points.Select(p => new Point(p.X * width / 100, height - p.Y * height / 100))
                ),
                Stroke = color,
                StrokeThickness = 2
            };
            canvas.Children.Add(polyline);
        }

        if (vm.WpmPoints.Any())
            DrawPolyline(vm.WpmPoints.ToArray(), Brushes.LightGreen);
        if (vm.RawWpmPoints.Any())
            DrawPolyline(vm.RawWpmPoints.ToArray(), Brushes.Orange);
        if (vm.ErrorPoints.Any())
            DrawPolyline(vm.ErrorPoints.ToArray(), Brushes.Red);
    }

    private void CloseButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}