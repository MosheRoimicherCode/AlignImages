//using Microsoft.UI;
//using Microsoft.UI.Windowing;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Controls.Primitives;
//using Microsoft.UI.Xaml.Data;
//using Microsoft.UI.Xaml.Input;
//using Microsoft.UI.Xaml.Media;
//using Microsoft.UI.Xaml.Navigation;
//using Microsoft.Windows.Storage.Pickers;
//using ReadMetadata;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using System.Threading.Tasks;
//using System.Windows;
//using Windows.Foundation;
//using Windows.Foundation.Collections;
//using Windows.Graphics;
//using Windows.Storage;
//using static System.Net.WebRequestMethods;
//// To learn more about WinUI, the WinUI project structure,
//// and more about our project templates, see: http://aka.ms/winui-project-info.

//namespace AlignImages2
//{
//    /// <summary>
//    /// An empty window that can be used on its own or navigated to within a Frame.
//    /// </summary>
//    public sealed partial class MainWindow : Window
//    {
//        List<string> paths = new();
//        string folderPath = String.Empty;

//        public MainWindow()
//        {

//            InitializeComponent();
//            var appWindow = GetAppWindowForCurrentWindow();
//            if (appWindow != null)
//            {
//                appWindow.Resize(new SizeInt32(600, 400));
//            }

//        }

//        private AppWindow GetAppWindowForCurrentWindow()
//        {
//            // Helper function to get the AppWindow from the current Window instance
//            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
//            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
//            return AppWindow.GetFromWindowId(windowId);
//        }

//        private async void PickMultipleFilesFlyButton_Click(object sender, RoutedEventArgs e)
//        {
//            FilePickerButton.IsEnabled = false;

//            if (sender is MenuFlyoutItem menu)
//            {

//                var picker = new FileOpenPicker(menu.XamlRoot.ContentIslandEnvironment.AppWindowId);

//                picker.CommitButtonText = "Pick Files";

//                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

//                picker.ViewMode = PickerViewMode.List;

//                // Show the picker dialog window
//                var files = await picker.PickMultipleFilesAsync();

//                if (files.Count > 0)
//                {
//                    PickedMultipleFilesTextBlock.Text = files.Count.ToString();
//                    foreach (var file in files)
//                    {
//                        paths.Add(file.Path);
//                    }
//                }
//                else
//                {
//                    PickedMultipleFilesTextBlock.Text = "No files selected.";
//                }

//                folderPath = String.Empty;
//            }

//        }

//        private async void PickFolderFlyButton_Click(object sender, RoutedEventArgs e)
//        {
//            if (sender is MenuFlyoutItem menu)
//            {
//                // disable the button to avoid double-clicking
//                menu.IsEnabled = false;
//                paths.Clear();  // Clear previous returned file names

//                // Clear previous returned folder name

//                var picker = new FolderPicker(menu.XamlRoot.ContentIslandEnvironment.AppWindowId);

//                picker.CommitButtonText = "Pick Folder";
//                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
//                picker.ViewMode = PickerViewMode.List;

//                // Show the picker dialog window
//                var folder = await picker.PickSingleFolderAsync();
//                folderPath = folder.Path;
//                FilePickerButton.IsEnabled = true;
//            }
//        }

//        private async void AlignImagesButton_Click(object sender, RoutedEventArgs e)
//        {
//            if (sender is Button button)
//            {


//                var progressHandler = new Progress<int>(value =>
//                {
//                    MyProgressBar.Value = value;
//                });

//                button.IsEnabled = false;
//                MyProgressBar.Visibility = Visibility.Visible;

//                ReadMetadata.Run run = new ReadMetadata.Run();

//                if (paths.Count > 0)
//                {
//                    await run.ExecuteFilesInput(paths, progressHandler);

//                }

//                else if (folderPath != string.Empty)
//                {
//                    await run.ExecuteFolderInput(folderPath, progressHandler);
//                }

//                // re-enable the button
//                MyProgressBar.Visibility = Visibility.Collapsed;
//                button.IsEnabled = true;
//                FilePickerButton.IsEnabled = true;


//                folderPath = String.Empty;
//                paths.Clear();

//            }

//        }
//    }
//}


using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            appWindow?.Resize(new SizeInt32(900, 720));
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

                LoadGeneratedPreviews(outputFolderToPreview);
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

            StorageFile file;
            try
            {
                file = await StorageFile.GetFileFromPathAsync(item.FilePath);
            }
            catch
            {
                return;
            }

            var bmp = new BitmapImage();
            using (var stream = await file.OpenReadAsync())
            {
                await bmp.SetSourceAsync(stream);
            }

            // Native pixel size (in DIPs for our purposes)
            double imgW = bmp.PixelWidth;
            double imgH = bmp.PixelHeight;

            var img = new Image
            {
                Source = bmp,
                Stretch = Microsoft.UI.Xaml.Media.Stretch.None,
                Width = imgW,
                Height = imgH
            };

            var sv = new ScrollViewer
            {
                ZoomMode = ZoomMode.Enabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = img
            };

            var dialog = new ContentDialog
            {
                Title = System.IO.Path.GetFileName(item.FilePath),
                PrimaryButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Root.XamlRoot,
                Content = sv
            };

            bool fittedOnce = false;

            void FitToWindow()
            {
                // ViewportWidth/Height are valid only after layout
                var vw = sv.ViewportWidth;
                var vh = sv.ViewportHeight;

                if (vw <= 0 || vh <= 0 || imgW <= 0 || imgH <= 0)
                    return;

                // Leave a tiny margin so it doesn't clip due to rounding
                var zoom = (float)Math.Min(vw / imgW, vh / imgH) * 0.98f;

                // Reasonable clamp
                if (zoom <= 0)
                    zoom = 0.01f;
                if (zoom > 10f)
                    zoom = 10f;

                sv.ChangeView(0, 0, zoom, disableAnimation: true);
                fittedOnce = true;
            }

            // Run fit after dialog is opened and layout exists
            dialog.Opened += (_, __) =>
            {
                // Post to UI queue to ensure ViewportWidth/Height are updated
                DispatcherQueue.TryEnqueue(() => FitToWindow());
            };

            // If the dialog size changes (user resizes window), refit
            sv.SizeChanged += (_, __) =>
            {
                if (fittedOnce)
                    FitToWindow();
            };

            await dialog.ShowAsync();
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

    }

    public class PreviewImageItem
    {
        public string FilePath { get; set; } = "";
        public BitmapImage Thumbnail { get; set; } = new();
    }
}
