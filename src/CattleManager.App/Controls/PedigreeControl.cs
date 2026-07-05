using CattleManager.Core.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CattleManager.App.Controls;

public class AddParentEventArgs(int childAnimalId, string role, string childBarnName) : EventArgs
{
    public int ChildAnimalId { get; } = childAnimalId;
    public string Role { get; } = role;
    public string ChildBarnName { get; } = childBarnName;
}

public class RemoveParentEventArgs(int childAnimalId, string role, string barnName) : EventArgs
{
    public int ChildAnimalId { get; } = childAnimalId;
    public string Role { get; } = role;
    public string BarnName { get; } = barnName;
}

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

    public event EventHandler<PedigreeNodeDto>?    NodeClicked;
    public event EventHandler<AddParentEventArgs>?    AddParentRequested;
    public event EventHandler<RemoveParentEventArgs>? RemoveParentRequested;

    private const double NodeWidth  = 140;
    private const double NodeHeight = 80;
    private const double HGap       = 40;   // widened slightly to fit action buttons
    private const double VGap       = 16;
    private const double BtnSize    = 22;
    private const double BtnOffset  = 4;    // gap between node right edge and buttons
    private const int    MaxGen     = 4;

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
        RenderNode(RootNode, 0, 0, totalHeight);

        int maxGen = GetMaxGeneration(RootNode);
        Width  = (maxGen + 1) * (NodeWidth + HGap) + HGap;
        Height = totalHeight + NodeHeight;
        InvalidateMeasure();
    }

    private int GetMaxGeneration(PedigreeNodeDto node)
    {
        int max = node.Generation;
        if (node.Sire is not null) max = Math.Max(max, GetMaxGeneration(node.Sire));
        if (node.Dam  is not null) max = Math.Max(max, GetMaxGeneration(node.Dam));
        return max;
    }

    protected override Size MeasureOverride(Size constraint)
    {
        base.MeasureOverride(constraint);
        return new Size(
            double.IsNaN(Width)  ? 0 : Width,
            double.IsNaN(Height) ? 0 : Height);
    }

    private double CalculateTotalHeight(PedigreeNodeDto node)
    {
        if (node.Sire is null && node.Dam is null)
            return NodeHeight + VGap;
        double h = 0;
        if (node.Sire is not null) h += CalculateTotalHeight(node.Sire);
        if (node.Dam  is not null) h += CalculateTotalHeight(node.Dam);
        return Math.Max(h, NodeHeight + VGap);
    }

    private Point RenderNode(PedigreeNodeDto node, int gen, double y, double height)
    {
        double x     = gen * (NodeWidth + HGap);
        double nodeY = y + height / 2 - NodeHeight / 2;

        var border = CreateNodeElement(node);
        SetLeft(border, x);
        SetTop(border, nodeY);
        Children.Add(border);

        // Action buttons float in the HGap to the right of the node box
        AddActionButtons(node, x, nodeY);

        var nodeCenterRight = new Point(x + NodeWidth, nodeY + NodeHeight / 2);

        if (node.Sire is not null || node.Dam is not null)
        {
            double sireHeight   = node.Sire is not null ? CalculateTotalHeight(node.Sire) : height / 2;
            double damHeight    = node.Dam  is not null ? CalculateTotalHeight(node.Dam)  : height / 2;
            double totalChildH  = sireHeight + damHeight;
            double childY       = y + height / 2 - totalChildH / 2;

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

    private void AddActionButtons(PedigreeNodeDto node, double nodeX, double nodeY)
    {
        double btnX = nodeX + NodeWidth + BtnOffset;

        // [+] Sire — add sire parent (available for any node with a real DB record)
        if (node.AnimalId.HasValue && node.Sire is null && node.Generation < MaxGen)
        {
            var btn = MakeActionButton("♂+", "#1565C0", "Assign Sire");
            SetLeft(btn, btnX);
            SetTop(btn, nodeY + 4);
            btn.Click += (_, e) =>
            {
                e.Handled = true;
                AddParentRequested?.Invoke(this, new AddParentEventArgs(
                    node.AnimalId!.Value, "Sire", node.BarnName ?? ""));
            };
            Children.Add(btn);
        }

        // [+] Dam — add dam parent (available for any node with a real DB record)
        if (node.AnimalId.HasValue && node.Dam is null && node.Generation < MaxGen)
        {
            var btn = MakeActionButton("♀+", "#AD1457", "Assign Dam");
            SetLeft(btn, btnX);
            SetTop(btn, nodeY + NodeHeight - BtnSize - 4);
            btn.Click += (_, e) =>
            {
                e.Handled = true;
                AddParentRequested?.Invoke(this, new AddParentEventArgs(
                    node.AnimalId!.Value, "Dam", node.BarnName ?? ""));
            };
            Children.Add(btn);
        }

        // [−] Remove — unlink this node from its biological child
        if (node.Generation > 0 && node.ChildAnimalId.HasValue)
        {
            var btn = MakeActionButton("−", "#555555", $"Remove {node.BarnName ?? "this animal"} from pedigree");
            SetLeft(btn, btnX);
            SetTop(btn, nodeY + NodeHeight / 2 - BtnSize / 2);
            btn.Click += (_, e) =>
            {
                e.Handled = true;
                RemoveParentRequested?.Invoke(this, new RemoveParentEventArgs(
                    node.ChildAnimalId!.Value, node.Role, node.BarnName ?? ""));
            };
            Children.Add(btn);
        }
    }

    private static Button MakeActionButton(string label, string hexColor, string tooltip)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        return new Button
        {
            Content    = label,
            Width      = BtnSize,
            Height     = BtnSize,
            FontSize   = 10,
            FontWeight = FontWeights.Bold,
            Padding    = new Thickness(0),
            Background = new SolidColorBrush(Color.FromArgb(200, color.R, color.G, color.B)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor   = Cursors.Hand,
            ToolTip  = tooltip,
            Tag      = hexColor   // unused, kept for debugging
        };
    }

    private Border CreateNodeElement(PedigreeNodeDto node)
    {
        var isUnknown   = !node.IsInHerd && node.AnimalId is null;
        var genderColor = node.Gender == Gender.Male
            ? Color.FromRgb(21, 101, 192)
            : node.Gender == Gender.Female
                ? Color.FromRgb(173, 20, 87)
                : Color.FromRgb(100, 100, 100);

        var border = new Border
        {
            Width           = NodeWidth,
            Height          = NodeHeight,
            CornerRadius    = new CornerRadius(8),
            BorderThickness = new Thickness(2),
            BorderBrush     = new SolidColorBrush(genderColor),
            Background      = isUnknown
                ? new SolidColorBrush(Color.FromArgb(30, 200, 200, 200))
                : new SolidColorBrush(Color.FromArgb(20, genderColor.R, genderColor.G, genderColor.B)),
            ClipToBounds    = true,
            Cursor          = node.IsInHerd ? Cursors.Hand : Cursors.Arrow,
            ToolTip         = BuildTooltip(node)
        };

        var hasPhoto = !string.IsNullOrEmpty(node.PhotoPath) && File.Exists(node.PhotoPath);
        if (hasPhoto)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var photo = new PhotoPositionControl
            {
                ImagePath    = node.PhotoPath,
                OffsetX      = node.PhotoOffsetX,
                OffsetY      = node.PhotoOffsetY,
                CornerRadius = new CornerRadius(6, 0, 0, 6),
                IsInteractive = false
            };
            Grid.SetColumn(photo, 0);
            grid.Children.Add(photo);

            var text = BuildTextStack(node, isUnknown, genderColor, new Thickness(6, 6, 6, 6));
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            border.Child = grid;
        }
        else
        {
            border.Child = BuildTextStack(node, isUnknown, genderColor, new Thickness(8, 6, 8, 6));
        }

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

    private static StackPanel BuildTextStack(PedigreeNodeDto node, bool isUnknown, Color genderColor, Thickness margin)
    {
        var stack = new StackPanel { Margin = margin };

        stack.Children.Add(new TextBlock
        {
            Text         = node.BarnName ?? "Unknown",
            FontWeight   = FontWeights.SemiBold,
            FontSize     = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground   = isUnknown ? Brushes.Gray : Brushes.Black
        });

        if (!string.IsNullOrEmpty(node.RegisteredName))
        {
            stack.Children.Add(new TextBlock
            {
                Text         = node.RegisteredName,
                FontSize     = 10,
                Foreground   = Brushes.Gray,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        stack.Children.Add(new TextBlock
        {
            Text                = node.Gender == Gender.Male ? "♂" : node.Gender == Gender.Female ? "♀" : "?",
            FontSize            = 14,
            Foreground          = new SolidColorBrush(genderColor),
            HorizontalAlignment = HorizontalAlignment.Right
        });

        return stack;
    }

    private static ToolTip BuildTooltip(PedigreeNodeDto node)
    {
        var tp    = new ToolTip();
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
        Children.Add(new Polyline
        {
            Stroke          = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
            StrokeThickness = 1.5,
            Points          = new PointCollection { from, new(midX, from.Y), new(midX, to.Y), to }
        });
    }
}
