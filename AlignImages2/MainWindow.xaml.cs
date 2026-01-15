using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using ReadMetadata;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.Storage;
using static System.Net.WebRequestMethods;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlignImages2
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        List<string> paths = new();
        string folderPath = String.Empty;

        public MainWindow()
        {
            
            InitializeComponent();
            var appWindow = GetAppWindowForCurrentWindow();
            if (appWindow != null)
            {
                appWindow.Resize(new SizeInt32(600, 400));
            }
            // Example: Add items to NavLinks if needed
            // NavLinks.Add(new { Title = "Home" });
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            // Helper function to get the AppWindow from the current Window instance
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        private async void PickMultipleFilesFlyButton_Click(object sender, RoutedEventArgs e)
        {
            FilePickerButton.IsEnabled = false;

            if (sender is MenuFlyoutItem menu)
            {

                var picker = new FileOpenPicker(menu.XamlRoot.ContentIslandEnvironment.AppWindowId);

                picker.CommitButtonText = "Pick Files";

                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                picker.ViewMode = PickerViewMode.List;

                // Show the picker dialog window
                var files = await picker.PickMultipleFilesAsync();

                if (files.Count > 0)
                {
                    PickedMultipleFilesTextBlock.Text = files.Count.ToString();
                    foreach (var file in files)
                    {
                        paths.Add(file.Path);
                    }
                }
                else
                {
                    PickedMultipleFilesTextBlock.Text = "No files selected.";
                }
            }

        }

        private async void PickFolderFlyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menu)
            {
                // disable the button to avoid double-clicking
                menu.IsEnabled = false;

                // Clear previous returned folder name

                var picker = new FolderPicker(menu.XamlRoot.ContentIslandEnvironment.AppWindowId);

                picker.CommitButtonText = "Pick Folder";
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.ViewMode = PickerViewMode.List;

                // Show the picker dialog window
                var folder = await picker.PickSingleFolderAsync();
                FilePickerButton.IsEnabled = true;
            }
        }
        
        private async void AlignImagesButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {

                var progressHandler = new Progress<int>(value =>
                {
                    // This code runs on the UI Thread automatically!
                    MyProgressBar.Value = value;
                });

                button.IsEnabled = false;
                MyProgressBar.Visibility = Visibility.Visible;

                ReadMetadata.Run run = new ReadMetadata.Run();
                
                if (paths.Count > 0)
                {
                    await run.ExecuteFilesInput(paths, progressHandler);
                    
                }

                else if (folderPath != string.Empty)
                {
                    await run.ExecuteFodlerInput(folderPath);
                }

                // re-enable the button
                MyProgressBar.Visibility = Visibility.Collapsed;
                button.IsEnabled = true;
                FilePickerButton.IsEnabled = true;
            }

        }
    }
}
