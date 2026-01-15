using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using ReadMetadata;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;


namespace DroneRotatorWinUI.ViewModels;

// This class uses the CommunityToolkit MVVM source generators to automatically 
// create the INotifyPropertyChanged code for us.
public partial class ImageItemViewModel : ObservableObject
{
    public string FullPath { get; private set; }

    [ObservableProperty]
    private string fileName;

    // The actual image shown in the table
    [ObservableProperty]
    private BitmapImage thumbnailImage;

    [ObservableProperty]
    private string detectedRoll;

    [ObservableProperty]
    private string pitchMode;

    [ObservableProperty]
    private string actionNeeded;

    [ObservableProperty]
    private string status;

    // To store the calculated rotation steps hidden from UI
    public int RotationSteps { get; set; }

    public ImageItemViewModel(string path)
    {
        FullPath = path;
        FileName = Path.GetFileName(path);
        Status = "Pending Analysis...";
    }

    // Asynchronously loads data so the UI doesn't freeze
    public async Task LoadDataAsync()
    {
        Status = "Analyzing...";
        await LoadThumbnailAsync();
        await ReadMetadataAsync();
    }

    private async Task LoadThumbnailAsync()
    {
        try
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(FullPath);
            // Get a small thumbnail efficiently from Windows
            StorageItemThumbnail thumbStream = await file.GetThumbnailAsync(ThumbnailMode.PicturesView, 100);
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(thumbStream);
            ThumbnailImage = bitmap;
        }
        catch { /* Handle placeholder if loading fails */ }
    }

    private async Task ReadMetadataAsync()
    {
        // Run heavy metadata reading on a background thread
        await Task.Run(() =>
        {
            try
            {
                // 1. Read orientation using our helper class
                var result = CameraOrientationReader.GetOrientation(FullPath);

                // 2. Calculate rotation logic using our foolproof logic class
                RotationSteps = result.rotation.RotationSteps;

                // Update UI properties (must be done on UI thread in older frameworks, 
                // but MVVM Toolkit handles this nicely)
                PitchMode = result.rotation.ModeDetected.Contains("Nadir") ? "Nadir (Map)" : "Horizon (View)";
                DetectedRoll = result.Roll.HasValue ? $"{result.Roll.Value:F1}°" : "N/A";

                ActionNeeded = RotationSteps switch
                {
                    0 => "No Change",
                    1 => "Rotate 90° CW ↻",
                    2 => "Rotate 180° ↷",
                    3 => "Rotate 90° CCW ↺",
                    _ => "Error"
                };

                Status = "Ready";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
                ActionNeeded = "Failed";
            }
        });
    }
}