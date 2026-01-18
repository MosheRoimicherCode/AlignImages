using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    
namespace ReadMetadata;

public class Run
{
    string outputFolder = string.Empty;
    public async Task ExecuteFolderInput(string inputFolder, IProgress<int> progress, Action<string>? onOutputImageCreated = null)
    {
        if (string.IsNullOrEmpty(inputFolder) || !Directory.Exists(inputFolder))
        {
            System.Diagnostics.Debug.WriteLine("Directory does not exist.");
            return;
        }

        try
        {
            var supportedExtensions = new[] { ".jpg", ".jpeg" };
            List<string> files = Directory.GetFiles(inputFolder)
                                          .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                                          .ToList();

            if (files.Count > 0)
            {
                // קריאה לפונקציה הקיימת שמעבדת את הקבצים
                // ניתן להעביר null עבור ה-IProgress אם אין צורך במד התקדמות כרגע
                await ExecuteFilesInput(files, progress, onOutputImageCreated);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No JPG files found in the directory.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading folder: {ex.Message}");
        }
    }


    public async Task ExecuteFilesInput(List<string> files, IProgress<int> progress, Action<string>? onOutputImageCreated = null)
    {
        if (files == null || files.Count == 0)
            return;

        try
        {
            if (string.IsNullOrEmpty(outputFolder))
            {
                outputFolder = Path.GetDirectoryName(files[0]);
            }

            string finalOutputDir = Path.Combine(outputFolder, "Rotated_Images");
            Directory.CreateDirectory(finalOutputDir);

            int count = 0;
            int total = files.Count;
            foreach (var file in files)
            {
                int steps = CameraOrientationReader.GetOrientation(file).rotation.RotationSteps;

                // The UI stays responsive because this is awaited
                await JpegTranRotator.RotateAndSaveImageAsync(file, finalOutputDir, steps, onOutputImageCreated);
                count++;
                if (progress != null)
                {
                    int percentComplete = (int)((float)count / total * 100);
                    progress.Report(percentComplete);
                    System.Diagnostics.Debug.WriteLine($"Progress: {percentComplete}");
                }
                

                System.Diagnostics.Debug.WriteLine($"Processing file {count} of {total}");

            }
        }
        catch (Exception ex)
        {
            // Handle potential IO errors here
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
    }
}
