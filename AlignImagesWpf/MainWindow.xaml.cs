using Microsoft.Win32;
using ReadMetadata;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace AlignImagesWpf
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch runStopwatch = new();
        private readonly DispatcherTimer uiTimer = new();

        private string folderPath = string.Empty;
        private DateTimeOffset startTime;
        private bool hasStartTime;

        public MainWindow()
        {
            InitializeComponent();

            uiTimer.Interval = TimeSpan.FromSeconds(1);
            uiTimer.Tick += (_, _) => RefreshTimeFields((int)ProgressBar.Value);
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new OpenFolderDialog
            {
                Title = "Select a folder with images",
                Multiselect = false
            };

            if (picker.ShowDialog(this) == true)
            {
                folderPath = picker.FolderName;
                SelectedFolderTextBlock.Text = folderPath;
            }
        }

        private async void AlignImagesButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                SelectedFolderTextBlock.Text = "Select a valid folder first.";
                return;
            }

            SelectFolderButton.IsEnabled = false;
            AlignImagesButton.IsEnabled = false;

            ResetRunFields();
            StartRunClock();

            try
            {
                var run = new Run();
                var progressHandler = new Progress<int>(value =>
                {
                    int percent = Math.Clamp(value, 0, 100);
                    ProgressBar.Value = percent;
                    ProgressPercentTextBlock.Text = $"{percent} %";
                    RefreshTimeFields(percent);
                });

                await run.ExecuteFolderInput(folderPath, progressHandler);

                if (ProgressBar.Value < 100)
                {
                    ProgressBar.Value = 100;
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
                SelectFolderButton.IsEnabled = true;
                AlignImagesButton.IsEnabled = true;
            }
        }

        private void ResetRunFields()
        {
            ProgressBar.Value = 0;
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

            EndTimeValueTextBlock.Text = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
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
