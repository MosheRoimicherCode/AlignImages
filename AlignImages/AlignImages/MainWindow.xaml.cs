using DroneRotatorWinUI.ViewModels;
using Microsoft.UI.Composition.SystemBackdrops;
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
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AlignImages
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        private CameraOrientationReader reader = new ();

        // The collection holding the data for the table
        public ObservableCollection<ImageItemViewModel> ImageList { get; } = new();

        // Timer for the fake demo progress
        private DispatcherTimer _demoTimer;
        private int _demoProgressCount = 0;


        public MainWindow()
        {
            this.InitializeComponent();
            this.PersistenceId = "MainWindow";
            this.SystemBackdrop = new BlurredBackdrop();
            this.Title = "SkyAxis";
            
        }


        //private async void PickMultipleFilesButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (sender is Button button)
        //    {
        //        //disable the button to avoid double-clicking
        //        button.IsEnabled = false;

        //        var picker = new FileOpenPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);

        //        picker.CommitButtonText = "Pick Files";

        //        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        //        picker.ViewMode = PickerViewMode.List;

        //        // Show the picker dialog window
        //        var files = await picker.PickMultipleFilesAsync();

        //        if (files.Count > 0)
        //        {
        //            PickedMultipleFilesTextBlock.Text = "";
        //            foreach (var file in files)
        //            {
        //                var obj = CameraOrientationReader.GetOrientation(file.Path);
        //                //if (obj.Pitch <= -91 || obj.Pitch >= -89)
        //                PickedMultipleFilesTextBlock.Text += "Image: " + file.Path + " Pitch: " + obj.Pitch + " Yaw: " + obj.Yaw + " Roll: " + obj.Roll + " Ratatons: " + obj.rotation.RotationSteps + " Mode: " + obj.rotation.ModeDetected + Environment.NewLine;
        //            }

        //        }
        //        else
        //        {
        //            PickedMultipleFilesTextBlock.Text = "No files selected.";
        //        }

        //        //re-enable the button
        //        button.IsEnabled = true;
        //    }
        //}

        //private async void ImportFolder_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        //{
        //    var folderPicker = new FolderPicker();
        //    // WinUI 3 requirement for Pickers in desktop apps
        //    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        //    WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

        //    folderPicker.FileTypeFilter.Add("*");
        //    var folder = await folderPicker.PickSingleFolderAsync();

        //    if (folder != null)
        //    {
        //        ImageList.Clear();
        //        // Find jpg files
        //        var files = Directory.GetFiles(folder.Path, "*.*")
        //                             .Where(s => s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
        //                                         s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));

        //        foreach (var textFilePath in files)
        //        {
        //            // Create the ViewModel and add it to the list
        //            var vm = new ImageItemViewModel(textFilePath);
        //            ImageList.Add(vm);
        //            // Trigger the background data loading (metadata/thumbnail)
        //            _ = vm.LoadDataAsync();
        //        }

        //        SummaryText.Text = $"Found {ImageList.Count} images ready for analysis.";
        //    }
        //}

        //// Handle "Process Batch" click (FAKE DEMO)
        //private void ProcessBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (ImageList.Count == 0)
        //        return;

        //    // Setup UI for processing state
        //    ProcessBtn.IsEnabled = false;
        //    ProgressPanel.Visibility = Visibility.Visible;
        //    MainProgressBar.Value = 0;
        //    _demoProgressCount = 0;
        //    MainProgressBar.Maximum = ImageList.Count;

        //    // Start fake timer
        //    _demoTimer = new DispatcherTimer();
        //    _demoTimer.Interval = TimeSpan.FromMilliseconds(100); // Fast updates
        //    _demoTimer.Tick += DemoTimer_Tick;
        //    _demoTimer.Start();
        //}

        //private void DemoTimer_Tick(object sender, object e)
        //{
        //    _demoProgressCount++;
        //    MainProgressBar.Value = _demoProgressCount;

        //    // Calculate percentage for text
        //    int percent = (int)((double)_demoProgressCount / ImageList.Count * 100);
        //    ProgressText.Text = ($"Processing item {_demoProgressCount} of {ImageList.Count} ({percent}%)");

        //    // Fake updating the status of the row being processed
        //    if (_demoProgressCount - 1 < ImageList.Count)
        //    {
        //        ImageList[_demoProgressCount - 1].Status = "Processing...";
        //    }

        //    // Finish condition
        //    if (_demoProgressCount >= ImageList.Count)
        //    {
        //        _demoTimer.Stop();
        //        ProcessBtn.IsEnabled = true;
        //        ProgressText.Text = "Batch Complete!";
        //        // Reset statuses
        //        foreach (var img in ImageList)
        //            img.Status = "Done";
        //    }
        //}
    }

    public class BlurredBackdrop : CompositionBrushBackdrop
    {
        protected override Windows.UI.Composition.CompositionBrush CreateBrush(Windows.UI.Composition.Compositor compositor)
            => compositor.CreateHostBackdropBrush();
    }
}
