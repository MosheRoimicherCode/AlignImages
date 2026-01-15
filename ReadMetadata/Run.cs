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
    public Task ExecuteFodlerInput(string inputFolder)
    {
        // Placeholder for future implementation
        return Task.CompletedTask;
    }

    public async Task ExecuteFilesInput2(List<string> files)
    {
        if (files == null || files.Count == 0)
            return;

        // 1. Determine Output Folder (Default to the first file's directory if empty)
        if (string.IsNullOrEmpty(outputFolder))
        {
            outputFolder = Path.GetDirectoryName(files[0]);
        }

        // 2. Create the specific subfolder for results
        string finalOutputDir = Path.Combine(outputFolder, "Rotated_Images");
        Directory.CreateDirectory(finalOutputDir);

        // 3. Process the files
        foreach (var file in files)
        {
            // Get the rotation steps from your metadata reader
            int steps = CameraOrientationReader.GetOrientation(file).rotation.RotationSteps;

            // Only process if rotation is actually needed
            if (steps > 0)
            {
                // Call the simplified method we created earlier
                // Note: We 'await' so the images process one by one without freezing the UI
                bool success = await JpegTranRotator.RotateAndSaveImageAsync(file, finalOutputDir, steps);

                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to process: {file}");
                }
            }
        }
    }

    public async Task ExecuteFilesInput(List<string> files, IProgress<int> progress)
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
                await JpegTranRotator.RotateAndSaveImageAsync(file, finalOutputDir, steps);
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
