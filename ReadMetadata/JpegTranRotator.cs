using MetadataExtractor.Formats.Photoshop;
using ReadMetadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ReadMetadata;
public static class JpegTranRotator
{
    // Enum to track user choice for file conflicts
    private enum ConflictAction
    {
        AskUser,
        OverwriteAll,
        SkipAll
    }

    public static async Task<bool> RotateAndSaveImageAsync(string sourceFile, string outputFolder, int clockwiseSteps, Action<string>? onOutputImageCreated = null)
    {
        // 1. Basic Validation
        if (!File.Exists(sourceFile))
            return false;

        // 2. Prepare Paths
        string fileName = Path.GetFileName(sourceFile);
        string outputPath = Path.Combine(outputFolder, fileName);

        // 3. Ensure Output Directory exists
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // --- NEW LOGIC FOR ZERO STEPS ---
        if (clockwiseSteps == 0)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Copy the file to the new directory (true allows overwriting)
                    File.Copy(sourceFile, outputPath, true);
                    onOutputImageCreated?.Invoke(outputPath);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Copy Error: {ex.Message}");
                    return false;
                }
            });
        }
        // --------------------------------

        // 4. Map steps to jpegtran flags (only if steps > 0)
        string jpegTranPath = GetJpegTranPath();
        string rotationFlag = clockwiseSteps switch
        {
            1 => "-rotate 90",
            2 => "-rotate 180",
            3 => "-rotate 270",
            _ => null
        };

        if (string.IsNullOrEmpty(rotationFlag) || string.IsNullOrEmpty(jpegTranPath))
            return false;

        // 5. Run the jpegtran process
        return await Task.Run(() =>
        {
            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = jpegTranPath,
                    Arguments = $"{rotationFlag} -copy all -outfile \"{outputPath}\" \"{sourceFile}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                onOutputImageCreated?.Invoke(outputPath);
                return process?.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Rotation Error: {ex.Message}");
                return false;
            }
        });
    }

    public static void ProcessImages(List<RotationJob> jobs)
    {
        string jpegTranPath = GetJpegTranPath();
        if (string.IsNullOrEmpty(jpegTranPath))
            return;

        Console.WriteLine($"\n--- Starting Batch Processing using {Path.GetFileName(jpegTranPath)} ---");
        Console.WriteLine($"Total images to check: {jobs.Count}");

        ConflictAction currentAction = ConflictAction.AskUser;
        int successCount = 0;
        int skippedCount = 0;

        foreach (var job in jobs)
        {
            // 1. Optimize Rotation Logic
            // If steps is 0, we typically just copy the file or skip depending on requirements.
            // Here we assume we skip strictly non-rotated files to save time, 
            // OR you can use "0" to just strip metadata/clean, but usually we skip.
            if (job.ClockwiseSteps == 0)
            {
                // Optional: If you want to copy the file anyway to the output folder:
                // File.Copy(job.SourcePath, job.OutputPath, true);
                skippedCount++;
                continue;
            }

            // 2. Check for File Conflicts (Output file already exists)
            if (File.Exists(job.OutputPath))
            {
                if (currentAction == ConflictAction.SkipAll)
                {
                    Console.WriteLine($"[Skipped] {Path.GetFileName(job.OutputPath)} exists.");
                    skippedCount++;
                    continue;
                }

                if (currentAction == ConflictAction.AskUser)
                {
                    // Ask the user what to do
                    currentAction = PromptUserForConflict(job.OutputPath);

                    // Re-check based on the new decision
                    if (currentAction == ConflictAction.SkipAll)
                    {
                        skippedCount++;
                        continue;
                    }
                    if (currentAction == ConflictAction.AskUser) // User selected "Skip Just This One"
                    {
                        skippedCount++;
                        continue;
                    }
                    // If Overwrite or OverwriteAll, we proceed below.
                }
            }

            // 3. Prepare the Output Directory
            string outDir = Path.GetDirectoryName(job.OutputPath);
            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            // 4. Translate Steps to JpegTran Arguments
            // 1 -> 90, 2 -> 180, 3 -> 270
            string rotationFlag = "";
            switch (job.ClockwiseSteps)
            {
                case 1:
                    rotationFlag = "-rotate 90";
                    break;
                case 2:
                    rotationFlag = "-rotate 180";
                    break;
                case 3:
                    rotationFlag = "-rotate 270";
                    break; // Efficient single command
                default:
                    continue;
            }

            // 5. Execute Command
            // Command: jpegtran.exe -rotate 90 -copy all -outfile "output.jpg" "input.jpg"
            bool success = RunJpegTran(jpegTranPath, rotationFlag, job.SourcePath, job.OutputPath);

            if (success)
                successCount++;
            else
                Console.WriteLine($"[Error] Failed to rotate {Path.GetFileName(job.SourcePath)}");
        }

        Console.WriteLine("\n--- Processing Complete ---");
        Console.WriteLine($"Rotated: {successCount}");
        Console.WriteLine($"Skipped: {skippedCount}");
    }

    // Helper to interact with the console user
    private static ConflictAction PromptUserForConflict(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\nCONFLICT: The file '{fileName}' already exists in the output folder.");
        Console.ResetColor();
        Console.WriteLine("What do you want to do?");
        Console.WriteLine("[Y] Yes (Overwrite this one)");
        Console.WriteLine("[A] All (Overwrite ALL future conflicts)");
        Console.WriteLine("[N] No (Skip this one)");
        Console.WriteLine("[S] Skip All (Skip ALL future conflicts)");
        Console.Write("Choice: ");

        while (true)
        {
            var key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.Y)
            {
                Console.WriteLine("Yes (Overwrite)");
                return ConflictAction.AskUser; // "AskUser" here acts as "Just this one, ask again next time"
            }
            if (key == ConsoleKey.A)
            {
                Console.WriteLine("Overwrite All");
                return ConflictAction.OverwriteAll;
            }
            if (key == ConsoleKey.N)
            {
                Console.WriteLine("No (Skip)");
                return ConflictAction.AskUser; // This will trigger the skip logic in the loop
            }
            if (key == ConsoleKey.S)
            {
                Console.WriteLine("Skip All");
                return ConflictAction.SkipAll;
            }
        }
    }

    // Helper to run the EXE silently
    private static bool RunJpegTran(string toolExe, string rotateArgs, string input, string output)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = toolExe;
            // IMPORTANT: "copy all" preserves the DJI Metadata
            psi.Arguments = $"{rotateArgs} -copy all -outfile \"{output}\" \"{input}\"";
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true; // Run in background
            psi.RedirectStandardError = true; // Catch errors if any

            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    // Jpegtran might output warnings to Stderr even on success, 
                    // but ExitCode 0 usually means success.
                    string error = p.StandardError.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(error))
                        Console.WriteLine($"JpegTran Warning/Error: {error}");
                    return false;
                }
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"System Error running Jpegtran: {ex.Message}");
            return false;
        }
    }

    private static string GetJpegTranPath()
    {
        // 1. Look in the same folder as the running .exe (Bin/Debug)
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string path = Path.Combine(baseDir, "jpegtran.exe");

        if (File.Exists(path))
            return path;

        // 2. Fallback: Check if user put it in the Solution root (Scanning up 3 levels)
        // This is useful during debugging if you forgot to set "Copy to Output"
        // Move up from bin\Debug\net6.0\ -> Project Folder -> Solution Folder
        DirectoryInfo dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 4; i++)
        {
            if (dir == null)
                break;
            string checkPath = Path.Combine(dir.FullName, "jpegtran.exe");
            if (File.Exists(checkPath))
                return checkPath;
            dir = dir.Parent;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"CRITICAL ERROR: 'jpegtran.exe' was not found!");
        Console.WriteLine($"Please put 'jpegtran.exe' inside: {baseDir}");
        Console.ResetColor();
        return null;
    }
}
