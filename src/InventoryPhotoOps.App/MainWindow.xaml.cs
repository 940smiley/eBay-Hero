using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using InventoryPhotoOps.App.ViewModels;

namespace InventoryPhotoOps.App;

public partial class MainWindow : Window
{
    private Point? _ocrSelectionStart;

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    public MainViewModel ViewModel { get; }

    private void PhotoGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ClearOcrSelectionVisual();
        ViewModel.SelectedRows.Clear();
        foreach (var item in PhotoGrid.SelectedItems.OfType<PhotoGridRow>())
        {
            ViewModel.SelectedRows.Add(item);
        }

        ViewModel.RefreshMetadataEditorFromSelection();
    }

    private void PreviewImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
    {
        ViewModel.StatusText = "Preview failed: " + e.ErrorException.Message;
    }

    private void OcrSelectionCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (PreviewImageControl.Source is not BitmapSource)
        {
            return;
        }

        _ocrSelectionStart = e.GetPosition(OcrSelectionCanvas);
        OcrSelectionRectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(OcrSelectionRectangle, _ocrSelectionStart.Value.X);
        Canvas.SetTop(OcrSelectionRectangle, _ocrSelectionStart.Value.Y);
        OcrSelectionRectangle.Width = 0;
        OcrSelectionRectangle.Height = 0;
        OcrSelectionCanvas.CaptureMouse();
    }

    private void OcrSelectionCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_ocrSelectionStart is not { } start || !OcrSelectionCanvas.IsMouseCaptured)
        {
            return;
        }

        var current = e.GetPosition(OcrSelectionCanvas);
        var rect = BuildRect(start, current);
        Canvas.SetLeft(OcrSelectionRectangle, rect.Left);
        Canvas.SetTop(OcrSelectionRectangle, rect.Top);
        OcrSelectionRectangle.Width = rect.Width;
        OcrSelectionRectangle.Height = rect.Height;
    }

    private void OcrSelectionCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_ocrSelectionStart is not { } start)
        {
            return;
        }

        var rect = BuildRect(start, e.GetPosition(OcrSelectionCanvas));
        _ocrSelectionStart = null;
        OcrSelectionCanvas.ReleaseMouseCapture();

        if (rect.Width < 8 || rect.Height < 8)
        {
            ClearOcrSelectionVisual();
            return;
        }

        if (!TryMapSelectionToImageCrop(rect, out var cropJson, out var summary))
        {
            ClearOcrSelectionVisual();
            ViewModel.StatusText = "OCR region selection did not overlap the displayed image.";
            return;
        }

        ViewModel.SetOcrRegion(cropJson, summary);
    }

    private void ClearOcrSelectionVisual()
    {
        if (OcrSelectionRectangle is null)
        {
            return;
        }

        OcrSelectionRectangle.Visibility = Visibility.Collapsed;
        OcrSelectionRectangle.Width = 0;
        OcrSelectionRectangle.Height = 0;
        _ocrSelectionStart = null;
    }

    private bool TryMapSelectionToImageCrop(Rect selection, out string cropJson, out string summary)
    {
        cropJson = string.Empty;
        summary = string.Empty;
        if (PreviewImageControl.Source is not BitmapSource bitmap || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
        {
            return false;
        }

        var imageRect = GetRenderedImageRect(bitmap);
        var clipped = Rect.Intersect(selection, imageRect);
        if (clipped.IsEmpty || clipped.Width < 1 || clipped.Height < 1)
        {
            return false;
        }

        var x = (int)Math.Round((clipped.Left - imageRect.Left) / imageRect.Width * bitmap.PixelWidth);
        var y = (int)Math.Round((clipped.Top - imageRect.Top) / imageRect.Height * bitmap.PixelHeight);
        var width = (int)Math.Round(clipped.Width / imageRect.Width * bitmap.PixelWidth);
        var height = (int)Math.Round(clipped.Height / imageRect.Height * bitmap.PixelHeight);
        x = Math.Clamp(x, 0, bitmap.PixelWidth - 1);
        y = Math.Clamp(y, 0, bitmap.PixelHeight - 1);
        width = Math.Clamp(width, 1, bitmap.PixelWidth - x);
        height = Math.Clamp(height, 1, bitmap.PixelHeight - y);

        cropJson = JsonSerializer.Serialize(new OcrCropRectangle(x, y, width, height));
        summary = $"OCR region selected: x {x}, y {y}, {width}x{height}.";
        return true;
    }

    private Rect GetRenderedImageRect(BitmapSource bitmap)
    {
        var controlWidth = Math.Max(1, PreviewImageControl.ActualWidth);
        var controlHeight = Math.Max(1, PreviewImageControl.ActualHeight);
        var scale = Math.Min(controlWidth / bitmap.PixelWidth, controlHeight / bitmap.PixelHeight);
        var width = bitmap.PixelWidth * scale;
        var height = bitmap.PixelHeight * scale;
        return new Rect((controlWidth - width) / 2, (controlHeight - height) / 2, width, height);
    }

    private static Rect BuildRect(Point start, Point end) =>
        new(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y));

    public static BitmapImage? LoadBitmapNoLock(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private sealed record OcrCropRectangle(int X, int Y, int Width, int Height);
}
