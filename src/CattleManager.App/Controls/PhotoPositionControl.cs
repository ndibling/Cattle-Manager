using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CattleManager.App.Controls;

/// <summary>
/// Displays a photo fitted to its bounds (UniformToFill) with an optional drag-to-reposition
/// interaction. OffsetX/OffsetY (0–1) represent the anchor point in the source image that
/// maps to the center of the frame; 0.5,0.5 = centered (default).
/// </summary>
public class PhotoPositionControl : FrameworkElement
{
    #region Dependency Properties

    public static readonly DependencyProperty ImagePathProperty =
        DependencyProperty.Register(nameof(ImagePath), typeof(string), typeof(PhotoPositionControl),
            new PropertyMetadata(null, OnImagePathChanged));

    public static readonly DependencyProperty OffsetXProperty =
        DependencyProperty.Register(nameof(OffsetX), typeof(double), typeof(PhotoPositionControl),
            new PropertyMetadata(0.5, OnOffsetChanged, CoerceOffset));

    public static readonly DependencyProperty OffsetYProperty =
        DependencyProperty.Register(nameof(OffsetY), typeof(double), typeof(PhotoPositionControl),
            new PropertyMetadata(0.5, OnOffsetChanged, CoerceOffset));

    public static readonly DependencyProperty IsInteractiveProperty =
        DependencyProperty.Register(nameof(IsInteractive), typeof(bool), typeof(PhotoPositionControl),
            new PropertyMetadata(false, OnIsInteractiveChanged));

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(PhotoPositionControl),
            new PropertyMetadata(new CornerRadius(0), OnCornerRadiusChanged));

    public string? ImagePath
    {
        get => (string?)GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    public double OffsetX
    {
        get => (double)GetValue(OffsetXProperty);
        set => SetValue(OffsetXProperty, value);
    }

    public double OffsetY
    {
        get => (double)GetValue(OffsetYProperty);
        set => SetValue(OffsetYProperty, value);
    }

    public bool IsInteractive
    {
        get => (bool)GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    private static object CoerceOffset(DependencyObject d, object baseValue) =>
        Math.Clamp((double)baseValue, 0.0, 1.0);

    #endregion

    private readonly Border _clip;
    private readonly Image _image;
    private readonly TextBlock _hint;
    private readonly TranslateTransform _transform;
    private BitmapImage? _bitmap;

    private bool _isDragging;
    private Point _dragStart;
    private double _offsetXAtDragStart;
    private double _offsetYAtDragStart;

    public PhotoPositionControl()
    {
        _transform = new TranslateTransform();

        _image = new Image { Stretch = Stretch.None, RenderTransform = _transform };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.HighQuality);

        _hint = new TextBlock
        {
            Text = "Drag to reposition",
            FontSize = 11,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            Padding = new Thickness(6, 3, 6, 3),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 8),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };

        var grid = new Grid();
        grid.Children.Add(_image);
        grid.Children.Add(_hint);

        _clip = new Border { ClipToBounds = true, Child = grid };

        AddVisualChild(_clip);
        AddLogicalChild(_clip);

        SizeChanged += (_, _) => ApplyTransform();

        _clip.MouseEnter += OnMouseEnter;
        _clip.MouseLeave += OnMouseLeave;
        _clip.MouseLeftButtonDown += OnMouseDown;
        _clip.MouseMove += OnMouseMove;
        _clip.MouseLeftButtonUp += OnMouseUp;
    }

    protected override int VisualChildrenCount => 1;
    protected override Visual GetVisualChild(int index) => _clip;

    protected override Size MeasureOverride(Size available)
    {
        _clip.Measure(available);
        return _clip.DesiredSize;
    }

    protected override Size ArrangeOverride(Size final)
    {
        _clip.Arrange(new Rect(final));
        ApplyTransform();
        return final;
    }

    private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PhotoPositionControl)d).LoadImage((string?)e.NewValue);

    private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PhotoPositionControl)d).ApplyTransform();

    private static void OnIsInteractiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (PhotoPositionControl)d;
        ctrl._clip.Cursor = (bool)e.NewValue ? Cursors.SizeAll : null;
    }

    private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PhotoPositionControl)d)._clip.CornerRadius = (CornerRadius)e.NewValue;

    private void LoadImage(string? path)
    {
        _bitmap = null;
        _image.Source = null;
        _image.Width = double.NaN;
        _image.Height = double.NaN;

        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            _bitmap = bmp;
            _image.Source = bmp;
            ApplyTransform();
        }
        catch { }
    }

    private void ApplyTransform()
    {
        if (_bitmap is null || ActualWidth <= 0 || ActualHeight <= 0) return;

        double frameW = ActualWidth;
        double frameH = ActualHeight;
        double scale  = Math.Max(frameW / _bitmap.PixelWidth, frameH / _bitmap.PixelHeight);
        double scaledW = _bitmap.PixelWidth  * scale;
        double scaledH = _bitmap.PixelHeight * scale;

        _image.Width  = scaledW;
        _image.Height = scaledH;

        _transform.X = -OffsetX * Math.Max(0, scaledW - frameW);
        _transform.Y = -OffsetY * Math.Max(0, scaledH - frameH);
    }

    private (double overflowX, double overflowY) GetOverflow()
    {
        if (_bitmap is null || ActualWidth <= 0 || ActualHeight <= 0) return (0, 0);
        double scale = Math.Max(ActualWidth / _bitmap.PixelWidth, ActualHeight / _bitmap.PixelHeight);
        return (
            Math.Max(0, _bitmap.PixelWidth  * scale - ActualWidth),
            Math.Max(0, _bitmap.PixelHeight * scale - ActualHeight)
        );
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (IsInteractive && _bitmap != null)
            _hint.Visibility = Visibility.Visible;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _hint.Visibility = Visibility.Collapsed;
        if (_isDragging)
        {
            _isDragging = false;
            _clip.ReleaseMouseCapture();
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsInteractive || _bitmap is null) return;
        _isDragging = true;
        _dragStart = e.GetPosition(_clip);
        _offsetXAtDragStart = OffsetX;
        _offsetYAtDragStart = OffsetY;
        _clip.CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var pos = e.GetPosition(_clip);
        var (overflowX, overflowY) = GetOverflow();
        OffsetX = overflowX > 0 ? Math.Clamp(_offsetXAtDragStart - (pos.X - _dragStart.X) / overflowX, 0, 1) : 0.5;
        OffsetY = overflowY > 0 ? Math.Clamp(_offsetYAtDragStart - (pos.Y - _dragStart.Y) / overflowY, 0, 1) : 0.5;
        e.Handled = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        _clip.ReleaseMouseCapture();
        e.Handled = true;
    }
}
