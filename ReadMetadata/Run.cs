using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReadMetadata;

public class Run
{
    private static readonly string[] SupportedExtensions = [".jpg", ".jpeg"];
    private const int MaxParallelWorkers = 4;

    public async Task ExecuteFolderInput(string inputFolder, IProgress<int> progress, Action<string>? onOutputImageCreated = null)
    {
        if (string.IsNullOrEmpty(inputFolder) || !Directory.Exists(inputFolder))
        {
            System.Diagnostics.Debug.WriteLine("Directory does not exist.");
            return;
        }

        try
        {
            List<string> files = Directory.EnumerateFiles(inputFolder)
                .Where(file => SupportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();

            if (files.Count > 0)
            {
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
            string? inputFolder = Path.GetDirectoryName(files[0]);
            if (string.IsNullOrWhiteSpace(inputFolder))
            {
                System.Diagnostics.Debug.WriteLine("Could not determine output folder.");
                return;
            }

            string finalOutputDir = Path.Combine(inputFolder, "Rotated_Images");
            Directory.CreateDirectory(finalOutputDir);

            int total = files.Count;
            int completedCount = 0;
            int workerCount = Math.Min(Math.Max(1, Environment.ProcessorCount), MaxParallelWorkers);
            var options = new ParallelOptions { MaxDegreeOfParallelism = workerCount };

            await Parallel.ForEachAsync(files, options, async (file, _) =>
            {
                try
                {
                    int steps = CameraOrientationReader.GetOrientation(file).rotation.RotationSteps;
                    await JpegTranRotator.RotateAndSaveImageAsync(file, finalOutputDir, steps, onOutputImageCreated);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing '{file}': {ex.Message}");
                }
                finally
                {
                    int done = Interlocked.Increment(ref completedCount);
                    int percentComplete = (int)((double)done / total * 100);
                    progress?.Report(percentComplete);
                    System.Diagnostics.Debug.WriteLine($"Processing file {done} of {total}");
                }
            });

            progress?.Report(100);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
    }
}
