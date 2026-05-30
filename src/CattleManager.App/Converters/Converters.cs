using CattleManager.Core.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CattleManager.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is false ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AnimalStatus status)
            return status switch
            {
                AnimalStatus.Healthy => new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                AnimalStatus.BreedingFemale => new SolidColorBrush(Color.FromRgb(173, 20, 87)),
                AnimalStatus.BreedingMale => new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                AnimalStatus.Pregnant => new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                AnimalStatus.Weaned => new SolidColorBrush(Color.FromRgb(0, 131, 143)),
                AnimalStatus.ForSale => new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                AnimalStatus.Inactive => new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                _ => Brushes.Gray
            };
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class GenderToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Gender.Male ? new SolidColorBrush(Color.FromRgb(21, 101, 192))
                                : new SolidColorBrush(Color.FromRgb(173, 20, 87));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class GenderToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Gender.Male
            ? new SolidColorBrush(Color.FromRgb(21, 101, 192))
            : new SolidColorBrush(Color.FromRgb(173, 20, 87));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class OverdueToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromArgb(30, 245, 127, 23))
            : Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CountToWarningColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count > 0 ? Color.FromArgb(30, 245, 127, 23) : Colors.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PathToImageConverter : IValueConverter
{
    private static BitmapImage? _placeholder;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path && System.IO.File.Exists(path))
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(path, UriKind.Absolute);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            return img;
        }
        return GetPlaceholder();
    }

    private static BitmapImage GetPlaceholder()
    {
        if (_placeholder is not null) return _placeholder;
        _placeholder = new BitmapImage(new Uri("pack://application:,,,/Assets/cow-placeholder.png"));
        return _placeholder;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class IsInHerdToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.5;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class GenderToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Gender.Male ? "♂" : "♀";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
