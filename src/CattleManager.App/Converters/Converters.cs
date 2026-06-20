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
                AnimalStatus.Healthy  => new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                AnimalStatus.Pregnant => new SolidColorBrush(Color.FromRgb(106, 27, 154)),
                AnimalStatus.ForSale  => new SolidColorBrush(Color.FromRgb(230, 81, 0)),
                AnimalStatus.Sold     => new SolidColorBrush(Color.FromRgb(66, 66, 66)),
                AnimalStatus.Inactive => new SolidColorBrush(Color.FromRgb(117, 117, 117)),
                AnimalStatus.Deceased => new SolidColorBrush(Color.FromRgb(97, 97, 97)),
                AnimalStatus.Calf     => new SolidColorBrush(Color.FromRgb(0, 131, 143)),
                _ => Brushes.Gray
            };
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class AnimalStatusDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is AnimalStatus s && s == AnimalStatus.ForSale ? "For Sale" : value?.ToString() ?? "";

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
            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(path, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                return img;
            }
            catch { return DependencyProperty.UnsetValue; }
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

public class TransactionTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
            return type switch
            {
                TransactionType.Income => new SolidColorBrush(Color.FromRgb(46, 125, 50)),
                TransactionType.Expense => new SolidColorBrush(Color.FromRgb(198, 40, 40)),
                TransactionType.CapitalInflux => new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                _ => Brushes.Gray
            };
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
