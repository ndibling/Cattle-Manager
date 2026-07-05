using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CattleManager.App.Controls;

/// <summary>
/// Displays a photo at UniformToFill scale with optional drag-to-reposition.
/// OffsetX/OffsetY (0–1, default 0.5) are the anchor fractions of the scaled image
/// that align to the top-left of the frame: 0 = left/top edge, 1 = right/bottom edge.
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

    // Canvas lets the Image overflow its layout slot — Grid/Border would clip it to frame size.
    // Stretch.Fill makes the Image render at exactly _image.Width x _image.Height.
    // Canvas.SetLeft/Top position the (oversized) image so the right region is visible.
    // The outer Border.ClipToBounds=true provides the visual crop.
    private readonly Border _clip;
    private readonly Canvas _imageCanvas;
    private readonly Image _image;
    private readonly TextBlock _hint;
    private BitmapImage? _bitmap;
    private double _scaledW, _scaledH;

    private bool _isDragging;
    private Point _dragStart;
    private double _offsetXAtDragStart;
    private double _offsetYAtDragStart;

    public PhotoPositionControl()
    {
        _image = new Image { Stretch = Stretch.Fill };
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

        // Canvas is the key: it arranges children at their DesiredSize rather than
        // constraining them to the Canvas's own layout slot, so the Image can be
        // 240 px wide inside a 120 px frame and still render at 240 px.
        _imageCanvas = new Canvas();
        _imageCanvas.Children.Add(_image);

        // Grid overlays the hint over the canvas without affecting image sizing.
        var overlay = new Grid();
        overlay.Children.Add(_imageCanvas);
        overlay.Children.Add(_hint);

        _clip = new Border { ClipToBounds = true, Child = overlay };

        AddVisualChild(_clip);
        AddLogicalChild(_clip);

        SizeChanged += (_, _) => UpdateScaling();

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
        UpdateScaling();
        return final;
    }

    private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PhotoPositionControl)d).LoadImage((string?)e.NewValue);

    private static void OnOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PhotoPositionControl)d).Reposition();

    private static void OnIsInteractiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PhotoPositionControl)d)._clip.Cursor = (bool)e.NewValue ? Cursors.SizeAll : null;

    private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PhotoPositionControl)d)._clip.CornerRadius = (CornerRadius)e.NewValue;

    private void LoadImage(string? path)
    {
        _bitmap = null;
        _scaledW = 0;
        _scaledH = 0;
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
            UpdateScaling();
        }
        catch { }
    }

    // Computes the scale and sets the Image's explicit Width/Height.
    // Must run after both the bitmap and the frame size are known.
    private void UpdateScaling()
    {
        if (_bitmap is null || ActualWidth <= 0 || ActualHeight <= 0) return;

        double frameW = ActualWidth;
        double frameH = ActualHeight;

        // UniformToFill guarantees the frame is covered, but one dimension always
        // fills exactly — meaning zero overflow and no panning in that direction.
        // Add a minimum pad so both dimensions always overflow the frame, enabling
        // panning in both X and Y regardless of photo orientation vs. frame shape.
        double fillScale = Math.Max(frameW / _bitmap.PixelWidth, frameH / _bitmap.PixelHeight);
        double pad       = Math.Min(frameW, frameH) * 0.15;
        double scale     = Math.Max(fillScale,
                           Math.Max((frameW + pad) / _bitmap.PixelWidth,
                                    (frameH + pad) / _bitmap.PixelHeight));

        _scaledW = _bitmap.PixelWidth  * scale;
        _scaledH = _bitmap.PixelHeight * scale;

        _image.Width  = _scaledW;
        _image.Height = _scaledH;

        Reposition();
    }

    // Moves the (oversized) image within the Canvas so that the OffsetX/Y region is visible.
    private void Reposition()
    {
        if (_scaledW <= 0) return;
        Canvas.SetLeft(_image, -OffsetX * Math.Max(0, _scaledW - ActualWidth));
        Canvas.SetTop (_image, -OffsetY * Math.Max(0, _scaledH - ActualHeight));
    }

    private (double ox, double oy) Overflow() => (
        Math.Max(0, _scaledW - ActualWidth),
        Math.Max(0, _scaledH - ActualHeight)
    );

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
        var (ox, oy) = Overflow();
        if (ox > 0) OffsetX = Math.Clamp(_offsetXAtDragStart - (pos.X - _dragStart.X) / ox, 0, 1);
        if (oy > 0) OffsetY = Math.Clamp(_offsetYAtDragStart - (pos.Y - _dragStart.Y) / oy, 0, 1);
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
