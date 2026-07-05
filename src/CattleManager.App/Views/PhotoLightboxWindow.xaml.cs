using CattleManager.Core.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CattleManager.App.Views;

public partial class PhotoLightboxWindow : Window
{
    private readonly List<AnimalPhotoDto> _photos;
    private int _currentIndex;

    public PhotoLightboxWindow(List<AnimalPhotoDto> photos, AnimalPhotoDto startPhoto)
    {
        InitializeComponent();
        _photos = photos;
        _currentIndex = _photos.FindIndex(p => p.AnimalPhotoId == startPhoto.AnimalPhotoId);
        if (_currentIndex < 0) _currentIndex = 0;
        ShowCurrentPhoto();
    }

    private void ShowCurrentPhoto()
    {
        if (_photos.Count == 0) return;

        var photo = _photos[_currentIndex];

        if (System.IO.File.Exists(photo.FilePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource    = new Uri(photo.FilePath, UriKind.Absolute);
                bmp.CacheOption  = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                MainImage.Source = bmp;
            }
            catch
            {
                MainImage.Source = null;
            }
        }
        else
        {
            MainImage.Source = null;
        }

        CountLabel.Text   = $"{_currentIndex + 1} of {_photos.Count}";
        DateLabel.Text    = photo.AddedDate != default
            ? photo.AddedDate.ToString("MMMM d, yyyy")
            : string.Empty;
        CaptionLabel.Text = photo.Caption ?? string.Empty;

        PrevButton.IsEnabled = _currentIndex > 0;
        NextButton.IsEnabled = _currentIndex < _photos.Count - 1;
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex > 0) { _currentIndex--; ShowCurrentPhoto(); }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_currentIndex < _photos.Count - 1) { _currentIndex++; ShowCurrentPhoto(); }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                if (_currentIndex > 0) { _currentIndex--; ShowCurrentPhoto(); }
                e.Handled = true;
                break;
            case Key.Right:
                if (_currentIndex < _photos.Count - 1) { _currentIndex++; ShowCurrentPhoto(); }
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
        }
    }
}
