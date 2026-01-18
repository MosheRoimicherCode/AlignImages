using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using ReadMetadata;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;

namespace AlignImages2
{
    public sealed partial class MainWindow : Window
    {
        private readonly List<string> paths = new();
        private string folderPath = string.Empty;
        private string outputPath;
        public ObservableCollection<PreviewImageItem> PreviewImages { get; } = new();

        public MainWindow()
        {
            InitializeComponent();

            PreviewGrid.ItemsSource = PreviewImages;
            PreviewImages.CollectionChanged += (_, __) =>
            {
                PreviewCountTextBlock.Text = $"{PreviewImages.Count} images";
            };

            var appWindow = GetAppWindowForCurrentWindow();
            appWindow?.Resize(new SizeInt32(900, 1200));
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        private async void PickMultipleFilesFlyButton_Click(object sender, RoutedEventArgs e)
        {
            FilePickerButton.IsEnabled = false;
            OpenOutputFolderButton.Visibility = Visibility.Collapsed;

            try
            {
                if (sender is not MenuFlyoutItem menu)
                    return;

                folderPath = string.Empty;
                paths.Clear();

                var picker = new FileOpenPicker(menu.XamlRoot.ContentIslandEnvironment.AppWindowId)
                {
                    CommitButtonText = "Pick Files",
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };

                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                //picker.FileTypeFilter.Add(".png");
                //picker.FileTypeFilter.Add(".tif");
                //picker.FileTypeFilter.Add(".tiff");
                //picker.FileTypeFilter.Add(".bmp");
                //picker.FileTypeFilter.Add(".webp");

                var files = await picker.PickMultipleFilesAsync();

                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                        paths.Add(file.Path);

                    PickedMultipleFilesTextBlock.Text = $"{files.Count} files selected";
                }
                else
                {
                    PickedMultipleFilesTextBlock.Text = "No files selected.";
                }
            }
            finally
            {
                FilePickerButton.IsEnabled = true;
            }
        }

        private async void PickFolderFlyButton_Click(object sender, RoutedEventArgs e)
        {
            OpenOutputFolderButton.Visibility = Visibility.Collapsed;

            if (sender is not MenuFlyoutItem menu)
                return;

            menu.IsEnabled = false;
            FilePickerButton.IsEnabled = false;

            try
            {
                paths.Clear();
                folderPath = string.Empty;

                var picker = new FolderPicker(menu.XamlRoot.ContentIslandEnvironment.AppWindowId)
                {
                    CommitButtonText = "Pick Folder",
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };

                var folder = await picker.PickSingleFolderAsync();
                if (folder == null)
                {
                    PickedMultipleFilesTextBlock.Text = "No folder selected.";
                    return;
                }

                folderPath = folder.Path;
                PickedMultipleFilesTextBlock.Text = $"Folder: {folder.Path}";
            }
            finally
            {
                menu.IsEnabled = true;
                FilePickerButton.IsEnabled = true;
            }
        }

        private async void AlignImagesButton_Click(object sender, RoutedEventArgs e)
        {
            if (paths.Count == 0 && string.IsNullOrWhiteSpace(folderPath))
            {
                PickedMultipleFilesTextBlock.Text = "Pick files or a folder first.";
                return;
            }

            AlignButton.IsEnabled = false;
            FilePickerButton.IsEnabled = false;

            MyProgressBar.Value = 0;
            MyProgressBar.Visibility = Visibility.Visible;

            PreviewImages.Clear();

            try
            {
                var progressHandler = new Progress<int>(value =>
                {
                    var v = Math.Max(0, Math.Min(100, value));
                    MyProgressBar.Value = v;
                });

                var run = new ReadMetadata.Run();

                Action<string> onCreated = (outPath) =>
                {
                    // Make sure UI thread
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        AddPreviewItem(outPath);
                    });
                };


                if (paths.Count > 0)
                {
                    outputPath = Path.Combine(Path.GetDirectoryName(paths.FirstOrDefault()), "Rotated_Images");
                    await run.ExecuteFilesInput(paths, progressHandler, onCreated);
                }
                else
                {
                    outputPath = Path.Combine(folderPath, "Rotated_Images");
                    await run.ExecuteFolderInput(folderPath, progressHandler, onCreated);
                }

                // חשוב: עדכן כאן את תיקיית הפלט האמיתית של הכלי שלך
                // ברירת מחדל: מציג תמונות מהתיקייה שנבחרה (או תיקיית הקבצים)
                string outputFolderToPreview =
                    !string.IsNullOrWhiteSpace(outputPath)
                        ? outputPath
                        : Path.GetDirectoryName(paths.First()) ?? string.Empty;

                //LoadGeneratedPreviews(outputFolderToPreview);
                if (!string.IsNullOrWhiteSpace(outputPath) && Directory.Exists(outputPath))
                {
                    OpenOutputFolderButton.Visibility = Visibility.Visible;
                }

            }
            catch (Exception ex)
            {
                PickedMultipleFilesTextBlock.Text = $"Error: {ex.Message}";
            }
            finally
            {
                MyProgressBar.Visibility = Visibility.Collapsed;
                AlignButton.IsEnabled = true;
                FilePickerButton.IsEnabled = true;

                folderPath = string.Empty;
                paths.Clear();
            }
        }

        private void LoadGeneratedPreviews(string outputFolderPath)
        {
            if (string.IsNullOrWhiteSpace(outputFolderPath) || !Directory.Exists(outputFolderPath))
                return;

            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff" };

            var files = Directory.EnumerateFiles(outputFolderPath)
                                 .Where(f => exts.Contains(Path.GetExtension(f)))
                                 .OrderBy(f => f)
                                 .ToList();

            PreviewImages.Clear();

            foreach (var filePath in files)
            {
                PreviewImages.Add(new PreviewImageItem
                {
                    FilePath = filePath,
                    Thumbnail = new BitmapImage(new Uri(filePath))
                });
            }
        }

        private async void PreviewGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not PreviewImageItem item || string.IsNullOrWhiteSpace(item.FilePath))
                return;

            try
            {
                var file = await StorageFile.GetFileFromPathAsync(item.FilePath);

                // Ask Windows to open it using the user's default photo app
                await Launcher.LaunchFileAsync(file);
            }
            catch (Exception ex)
            {
                PickedMultipleFilesTextBlock.Text = $"Open failed: {ex.Message}";
            }
        }

        private void AddPreviewItem(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            var ext = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(ext))
                return;

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff" };

            if (!allowed.Contains(ext))
                return;

            // Prevent duplicates
            if (PreviewImages.Any(x => string.Equals(x.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
                return;

            PreviewImages.Add(new PreviewImageItem
            {
                FilePath = filePath,
                Thumbnail = new BitmapImage(new Uri(filePath))
            });

            // Optional: auto-scroll to newest item
            PreviewGrid?.ScrollIntoView(PreviewImages.Last());
        }

        private async void OpenOutputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(outputPath) || !Directory.Exists(outputPath))
                {
                    PickedMultipleFilesTextBlock.Text = "Output folder not found.";
                    return;
                }

                var folder = await StorageFolder.GetFolderFromPathAsync(outputPath);
                await Launcher.LaunchFolderAsync(folder);
            }
            catch (Exception ex)
            {
                PickedMultipleFilesTextBlock.Text = $"Open folder failed: {ex.Message}";
            }
        }

    }

    public class PreviewImageItem
    {
        public string FilePath { get; set; } = "";
        public BitmapImage Thumbnail { get; set; } = new();
    }

    public sealed class PercentToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return "0 %";

            if (value is double d)
                return $"{Math.Round(d)} %";

            if (double.TryParse(value.ToString(), out var parsed))
                return $"{Math.Round(parsed)} %";

            return "0 %";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}
