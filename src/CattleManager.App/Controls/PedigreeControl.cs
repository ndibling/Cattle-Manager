using CattleManager.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CattleManager.App.Controls;

public class PedigreeControl : Canvas
{
    public static readonly DependencyProperty RootNodeProperty =
        DependencyProperty.Register(nameof(RootNode), typeof(PedigreeNodeDto), typeof(PedigreeControl),
            new PropertyMetadata(null, OnRootNodeChanged));

    public PedigreeNodeDto? RootNode
    {
        get => (PedigreeNodeDto?)GetValue(RootNodeProperty);
        set => SetValue(RootNodeProperty, value);
    }

    public event EventHandler<PedigreeNodeDto>? NodeClicked;

    private const double NodeWidth = 140;
    private const double NodeHeight = 80;
    private const double HGap = 30;
    private const double VGap = 16;

    private static void OnRootNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PedigreeControl control)
            control.Render();
    }

    private void Render()
    {
        Children.Clear();
        Background = Brushes.Transparent;
        if (RootNode is null) return;

        double totalHeight = CalculateTotalHeight(RootNode);

        // startY = 0 so RenderNode places the root at totalHeight/2 (centre).
        // Previously this was totalHeight/2 - NodeHeight/2, which caused a
        // double-offset: RenderNode adds height/2 internally, so the root
        // landed at totalHeight - NodeHeight (near the bottom) and ancestors
        // spilled past the canvas boundary.
        RenderNode(RootNode, 0, 0, totalHeight);

        int maxGen = GetMaxGeneration(RootNode);
        Width = (maxGen + 1) * (NodeWidth + HGap) + HGap;
        Height = totalHeight + NodeHeight;
        InvalidateMeasure();
    }

    private int GetMaxGeneration(PedigreeNodeDto node)
    {
        int max = node.Generation;
        if (node.Sire is not null) max = Math.Max(max, GetMaxGeneration(node.Sire));
        if (node.Dam is not null) max = Math.Max(max, GetMaxGeneration(node.Dam));
        return max;
    }

    // Canvas measures itself as (0,0) by default, which means the ScrollViewer
    // never learns the real size. Report our explicit dimensions so scrollbars
    // are sized correctly.
    protected override Size MeasureOverride(Size constraint)
    {
        base.MeasureOverride(constraint);
        return new Size(
            double.IsNaN(Width) ? 0 : Width,
            double.IsNaN(Height) ? 0 : Height);
    }

    private double CalculateTotalHeight(PedigreeNodeDto node)
    {
        if (node.Sire is null && node.Dam is null)
            return NodeHeight + VGap;
        double h = 0;
        if (node.Sire is not null) h += CalculateTotalHeight(node.Sire);
        if (node.Dam is not null) h += CalculateTotalHeight(node.Dam);
        return Math.Max(h, NodeHeight + VGap);
    }

    private Point RenderNode(PedigreeNodeDto node, int gen, double y, double height)
    {
        double x = gen * (NodeWidth + HGap);
        double nodeY = y + height / 2 - NodeHeight / 2;

        var border = CreateNodeElement(node);
        SetLeft(border, x);
        SetTop(border, nodeY);
        Children.Add(border);

        var nodeCenterRight = new Point(x + NodeWidth, nodeY + NodeHeight / 2);

        if (node.Sire is not null || node.Dam is not null)
        {
            double sireHeight = node.Sire is not null ? CalculateTotalHeight(node.Sire) : height / 2;
            double damHeight = node.Dam is not null ? CalculateTotalHeight(node.Dam) : height / 2;
            double totalChildH = sireHeight + damHeight;
            double childY = y + height / 2 - totalChildH / 2;

            if (node.Sire is not null)
            {
                var childCenter = RenderNode(node.Sire, gen + 1, childY, sireHeight);
                DrawLine(nodeCenterRight, childCenter);
                childY += sireHeight;
            }
            if (node.Dam is not null)
            {
                var childCenter = RenderNode(node.Dam, gen + 1, childY, damHeight);
                DrawLine(nodeCenterRight, childCenter);
            }
        }

        return new Point(x, nodeY + NodeHeight / 2);
    }

    private Border CreateNodeElement(PedigreeNodeDto node)
    {
        var isUnknown = !node.IsInHerd && node.AnimalId is null;
        var genderColor = node.Gender == Gender.Male
            ? Color.FromRgb(21, 101, 192)
            : node.Gender == Gender.Female
                ? Color.FromRgb(173, 20, 87)
                : Color.FromRgb(100, 100, 100);

        var border = new Border
        {
            Width = NodeWidth,
            Height = NodeHeight,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(genderColor),
            Background = isUnknown
                ? new SolidColorBrush(Color.FromArgb(30, 200, 200, 200))
                : new SolidColorBrush(Color.FromArgb(20, genderColor.R, genderColor.G, genderColor.B)),
            Cursor = node.IsInHerd ? Cursors.Hand : Cursors.Arrow,
            ToolTip = BuildTooltip(node)
        };

        var stack = new StackPanel { Margin = new Thickness(8, 6, 8, 6) };

        var nameText = new TextBlock
        {
            Text = node.BarnName ?? "Unknown",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = isUnknown ? Brushes.Gray : Brushes.Black
        };
        stack.Children.Add(nameText);

        if (!string.IsNullOrEmpty(node.RegisteredName))
        {
            stack.Children.Add(new TextBlock
            {
                Text = node.RegisteredName,
                FontSize = 10,
                Foreground = Brushes.Gray,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        var genderSymbol = new TextBlock
        {
            Text = node.Gender == Gender.Male ? "♂" : node.Gender == Gender.Female ? "♀" : "?",
            FontSize = 14,
            Foreground = new SolidColorBrush(genderColor),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        stack.Children.Add(genderSymbol);

        border.Child = stack;

        if (node.IsInHerd)
        {
            border.MouseLeftButtonDown += (s, e) =>
            {
                NodeClicked?.Invoke(this, node);
                e.Handled = true;
            };
        }

        return border;
    }

    private static ToolTip BuildTooltip(PedigreeNodeDto node)
    {
        var tp = new ToolTip();
        var stack = new StackPanel { Margin = new Thickness(8) };
        stack.Children.Add(new TextBlock { Text = node.BarnName ?? "Unknown", FontWeight = FontWeights.Bold });
        if (!string.IsNullOrEmpty(node.RegisteredName))
            stack.Children.Add(new TextBlock { Text = node.RegisteredName });
        if (!string.IsNullOrEmpty(node.BreedName))
            stack.Children.Add(new TextBlock { Text = $"Breed: {node.BreedName}" });
        if (!node.IsInHerd)
            stack.Children.Add(new TextBlock { Text = "Not in current herd", Foreground = Brushes.Gray });
        tp.Content = stack;
        return tp;
    }

    private void DrawLine(Point from, Point to)
    {
        var midX = (from.X + to.X) / 2;
        var line = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
            StrokeThickness = 1.5,
            Points = new PointCollection
            {
                from,
                new Point(midX, from.Y),
                new Point(midX, to.Y),
                to
            }
        };
        Children.Add(line);
    }
}
