using CattleManager.App.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CattleManager.App.Views;

public partial class BudgetChartControl : UserControl
{
    public static readonly DependencyProperty ChartDataProperty =
        DependencyProperty.Register(
            nameof(ChartData),
            typeof(IReadOnlyList<BudgetChartMonth>),
            typeof(BudgetChartControl),
            new PropertyMetadata(null, OnChartDataChanged));

    public IReadOnlyList<BudgetChartMonth>? ChartData
    {
        get => (IReadOnlyList<BudgetChartMonth>?)GetValue(ChartDataProperty);
        set => SetValue(ChartDataProperty, value);
    }

    private static void OnChartDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((BudgetChartControl)d).Draw();

    public BudgetChartControl() => InitializeComponent();

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Draw();

    private void Draw()
    {
        ChartCanvas.Children.Clear();

        var data = ChartData;
        if (data is null || data.Count == 0) return;

        double canvasW = ChartCanvas.ActualWidth;
        double canvasH = ChartCanvas.ActualHeight;
        if (canvasW < 40 || canvasH < 40) return;

        const double padLeft   = 64;
        const double padRight  = 16;
        const double padTop    = 12;
        const double padBottom = 28;

        double plotW = canvasW - padLeft - padRight;
        double plotH = canvasH - padTop - padBottom;

        // Max value for y-axis scaling
        double maxVal = (double)data.Max(d =>
            Math.Max(Math.Max(d.BudgetedIncome, d.ActualIncome),
                     Math.Max(d.BudgetedExpense, d.ActualExpense)));
        if (maxVal <= 0) maxVal = 1;

        double niceMax = NiceMax(maxVal);
        int    tickCount = 5;

        // Y-axis grid lines + labels
        var labelBrush = (Brush)FindResource("SystemControlForegroundBaseMediumBrush");
        var gridBrush  = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128));

        for (int t = 0; t <= tickCount; t++)
        {
            double fraction = (double)t / tickCount;
            double y = padTop + plotH - fraction * plotH;
            double val = fraction * niceMax;

            // Grid line
            var line = new Line
            {
                X1 = padLeft, Y1 = y,
                X2 = padLeft + plotW, Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1,
            };
            ChartCanvas.Children.Add(line);

            // Y label
            var label = MakeLabel(FormatCurrency(val), 10, labelBrush);
            ChartCanvas.Children.Add(label);
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetRight(label, canvasW - padLeft + 4);
            Canvas.SetTop(label, y - label.DesiredSize.Height / 2);
        }

        // Bars — 4 per month group
        var brushBudgetIncome  = new SolidColorBrush(Color.FromRgb(0x90, 0xCA, 0xF9));
        var brushActualIncome  = new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47));
        var brushBudgetExpense = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x80));
        var brushActualExpense = new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50));

        double groupW   = plotW / data.Count;
        double barGap   = Math.Max(1, groupW * 0.04);
        double barW     = (groupW - barGap * 5) / 4;
        if (barW < 2) barW = 2;

        for (int i = 0; i < data.Count; i++)
        {
            var d   = data[i];
            double gx = padLeft + i * groupW;

            DrawBar(gx + barGap,                           d.BudgetedIncome,  brushBudgetIncome,  plotH, padTop, niceMax);
            DrawBar(gx + barGap * 2 + barW,                d.ActualIncome,    brushActualIncome,  plotH, padTop, niceMax);
            DrawBar(gx + barGap * 3 + barW * 2,            d.BudgetedExpense, brushBudgetExpense, plotH, padTop, niceMax);
            DrawBar(gx + barGap * 4 + barW * 3,            d.ActualExpense,   brushActualExpense, plotH, padTop, niceMax);

            // Month label
            var ml = MakeLabel(d.Label, 10, labelBrush);
            ChartCanvas.Children.Add(ml);
            ml.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(ml, gx + groupW / 2 - ml.DesiredSize.Width / 2);
            Canvas.SetTop(ml, padTop + plotH + 6);
        }

        void DrawBar(double x, decimal value, Brush brush, double ph, double pt, double nmax)
        {
            if (value <= 0) return;
            double barH = (double)value / nmax * ph;
            var rect = new Rectangle
            {
                Width  = barW,
                Height = barH,
                Fill   = brush,
                RadiusX = 2, RadiusY = 2,
            };
            ChartCanvas.Children.Add(rect);
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, pt + ph - barH);
        }
    }

    private static TextBlock MakeLabel(string text, double fontSize, Brush foreground) => new()
    {
        Text       = text,
        FontSize   = fontSize,
        Foreground = foreground,
    };

    private static double NiceMax(double rawMax)
    {
        if (rawMax <= 0) return 1;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawMax)));
        double normalized = rawMax / magnitude;
        double nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
        return nice * magnitude;
    }

    private static string FormatCurrency(double value)
    {
        if (value >= 1_000_000) return $"${value / 1_000_000:0.#}M";
        if (value >= 1_000)     return $"${value / 1_000:0.#}K";
        return $"${value:0}";
    }
}
