using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using ReadMetadata;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Graphics;

namespace AlignImages2
{
    public sealed partial class MainWindow : Window
    {
        private readonly Stopwatch runStopwatch = new();
        private readonly DispatcherTimer uiTimer = new();

        private string folderPath = string.Empty;
        private DateTimeOffset startTime;
        private bool hasStartTime;

        public MainWindow()
        {
            InitializeComponent();

            var appWindow = GetAppWindowForCurrentWindow();
            appWindow?.Resize(new SizeInt32(700, 520));

            uiTimer.Interval = TimeSpan.FromSeconds(1);
            uiTimer.Tick += (_, __) => RefreshTimeFields((int)MyProgressBar.Value);
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            SelectFolderButton.IsEnabled = false;

            try
            {
                var picker = new FolderPicker(SelectFolderButton.XamlRoot.ContentIslandEnvironment.AppWindowId)
                {
                    CommitButtonText = "Pick Folder",
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    ViewMode = PickerViewMode.List
                };

                var folder = await picker.PickSingleFolderAsync();
                if (folder == null)
                {
                    SelectedFolderTextBlock.Text = "No folder selected.";
                    folderPath = string.Empty;
                    return;
                }

                folderPath = folder.Path;
                SelectedFolderTextBlock.Text = folderPath;
            }
            finally
            {
                SelectFolderButton.IsEnabled = true;
            }
        }

        private async void AlignImagesButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                SelectedFolderTextBlock.Text = "Select a valid folder first.";
                return;
            }

            AlignButton.IsEnabled = false;
            SelectFolderButton.IsEnabled = false;

            ResetRunFields();
            StartRunClock();

            try
            {
                var run = new Run();
                var progressHandler = new Progress<int>(value =>
                {
                    int percent = Math.Max(0, Math.Min(100, value));
                    MyProgressBar.Value = percent;
                    ProgressPercentTextBlock.Text = $"{percent} %";
                    RefreshTimeFields(percent);
                });

                await run.ExecuteFolderInput(folderPath, progressHandler);

                if (MyProgressBar.Value < 100)
                {
                    MyProgressBar.Value = 100;
                    ProgressPercentTextBlock.Text = "100 %";
                }
            }
            catch (Exception ex)
            {
                SelectedFolderTextBlock.Text = $"Error: {ex.Message}";
            }
            finally
            {
                EndRunClock();
                AlignButton.IsEnabled = true;
                SelectFolderButton.IsEnabled = true;
            }
        }

        private void ResetRunFields()
        {
            MyProgressBar.Value = 0;
            ProgressPercentTextBlock.Text = "0 %";

            StartTimeValueTextBlock.Text = "-";
            EndTimeValueTextBlock.Text = "-";
            ElapsedValueTextBlock.Text = "00:00:00";
            EstimatedTotalValueTextBlock.Text = "-";
            EstimatedRemainingValueTextBlock.Text = "-";

            hasStartTime = false;
            runStopwatch.Reset();
            uiTimer.Stop();
        }

        private void StartRunClock()
        {
            startTime = DateTimeOffset.Now;
            hasStartTime = true;

            StartTimeValueTextBlock.Text = startTime.ToString("yyyy-MM-dd HH:mm:ss");
            EndTimeValueTextBlock.Text = "-";

            runStopwatch.Restart();
            uiTimer.Start();
        }

        private void EndRunClock()
        {
            if (!hasStartTime)
                return;

            uiTimer.Stop();
            runStopwatch.Stop();

            var endTime = DateTimeOffset.Now;
            EndTimeValueTextBlock.Text = endTime.ToString("yyyy-MM-dd HH:mm:ss");

            ElapsedValueTextBlock.Text = FormatDuration(runStopwatch.Elapsed);
            EstimatedTotalValueTextBlock.Text = FormatDuration(runStopwatch.Elapsed);
            EstimatedRemainingValueTextBlock.Text = "00:00:00";
        }

        private void RefreshTimeFields(int percent)
        {
            if (!hasStartTime)
                return;

            var elapsed = runStopwatch.Elapsed;
            ElapsedValueTextBlock.Text = FormatDuration(elapsed);

            if (percent <= 0)
            {
                EstimatedTotalValueTextBlock.Text = "-";
                EstimatedRemainingValueTextBlock.Text = "-";
                return;
            }

            if (percent >= 100)
            {
                EstimatedTotalValueTextBlock.Text = FormatDuration(elapsed);
                EstimatedRemainingValueTextBlock.Text = "00:00:00";
                return;
            }

            double fraction = percent / 100.0;
            var estimatedTotal = TimeSpan.FromSeconds(elapsed.TotalSeconds / fraction);
            var remaining = estimatedTotal - elapsed;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            EstimatedTotalValueTextBlock.Text = FormatDuration(estimatedTotal);
            EstimatedRemainingValueTextBlock.Text = FormatDuration(remaining);
        }

        private static string FormatDuration(TimeSpan value)
        {
            int hours = (int)value.TotalHours;
            return $"{hours:00}:{value.Minutes:00}:{value.Seconds:00}";
        }
    }
}
