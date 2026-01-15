using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace ReadMetadata;

public class CameraOrientationReader
{
    public struct RotationResult
    {
        public int RotationSteps; // 0, 1, 2, 3 (Clockwise 90-degree steps)
        public string ModeDetected; // "Map Mode" or "Horizon Mode"
    }
    public struct DroneOrientation
    {
        public double? Yaw;   // Compass heading (0-360)
        public double? Pitch; // Tilt (+90 up, -90 down)
        public double? Roll;  // Horizon angle
        public string Description;
        public RotationResult rotation;
    }

    public static DroneOrientation GetOrientation(string imagePath)
    {
        var result = new DroneOrientation();

        try
        {
            // 1. Read all metadata from the file
            var directories = ImageMetadataReader.ReadMetadata(imagePath);

            // 2. Look for XMP Directory (Where DJI/Autel store Gimbal data)
            var xmpDirectory = directories.OfType<XmpDirectory>().FirstOrDefault();

            if (xmpDirectory != null && xmpDirectory.XmpMeta != null)
            {
                // We access the XMP properties directly using the namespaces usually used by DJI
                // Note: Different drones use slightly different schema names, but this is the standard DJI one.
                try
                {
                    var pitchStr = xmpDirectory.XmpMeta.GetProperty("http://www.dji.com/drone-dji/1.0/", "GimbalPitchDegree");
                    var yawStr = xmpDirectory.XmpMeta.GetProperty("http://www.dji.com/drone-dji/1.0/", "GimbalYawDegree");
                    var rollStr = xmpDirectory.XmpMeta.GetProperty("http://www.dji.com/drone-dji/1.0/", "GimbalRollDegree");

                    if (pitchStr != null)
                        result.Pitch = double.Parse(pitchStr.Value);
                    if (yawStr != null)
                        result.Yaw = double.Parse(yawStr.Value);
                    if (rollStr != null)
                        result.Roll = double.Parse(rollStr.Value);
                }
                catch { /* XMP structure might vary, strictly handling DJI schema here */ }
            }

            // 3. Fallback: If no Drone XMP, try Standard GPS EXIF (Only gives Yaw/Heading)
            if (result.Yaw == null)
            {
                var gpsDir = directories.OfType<MetadataExtractor.Formats.Exif.GpsDirectory>().FirstOrDefault();
                if (gpsDir != null && gpsDir.TryGetDouble(MetadataExtractor.Formats.Exif.GpsDirectory.TagImgDirection, out double gpsHeading))
                {
                    result.Yaw = gpsHeading;
                }
            }

            // 4. Generate a human-readable description
            //result.Description = InterpretOrientation(result);

            var clockwiserotation = GetSmartRotation(result);

            result.rotation = clockwiserotation;

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
            return result;
        }
    }

    //private static string InterpretOrientation(DroneOrientation o)
    //{
    //    string desc = "";

    //    // Interpret Pitch (Tilt)
    //    if (o.Pitch.HasValue)
    //    {
    //        if (o.Pitch > 80)
    //            desc += "[LOOKING UP/ZENITH] ";
    //        else if (o.Pitch < -80)
    //            desc += "[LOOKING DOWN/NADIR] ";
    //        else if (o.Pitch > -10 && o.Pitch < 10)
    //            desc += "[LOOKING HORIZONTAL] ";
    //        else
    //            desc += $"[Angled {o.Pitch:F1}°] ";
    //    }

    //    // Interpret Roll (The "Upside Down" check)
    //    if (o.Roll.HasValue)
    //    {
    //        if (Math.Abs(o.Roll.Value) > 170)
    //            desc += "**UPSIDE DOWN CAMERA** ";
    //        else if (Math.Abs(o.Roll.Value) > 45)
    //            desc += "**HEAVY BANK/ROLL** ";
    //    }

    //    // Interpret Yaw (Compass)
    //    if (o.Yaw.HasValue)
    //    {
    //        desc += $"facing {GetCardinalDirection(o.Yaw.Value)} ({o.Yaw:F1}°)";
    //    }

    //    return string.IsNullOrEmpty(desc) ? "No orientation data found" : desc;
    //}

    private static string GetCardinalDirection(double degrees)
    {
        string[] caridnals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW", "N" };
        return caridnals[(int)Math.Round(((double)degrees % 360) / 45)];
    }

    public static int GetClockwiseRotations(DroneOrientation orient)
    {
        // If we don't have roll data, assume it's already upright (0 rotations)
        if (!orient.Roll.HasValue)
            return 0;

        double roll = orient.Roll.Value;

        // Normalize roll to -180 to +180 range (just in case the drone logs 0-360)
        if (roll > 180)
            roll -= 360;
        if (roll <= -180)
            roll += 360;

        // LOGIC: We map the Roll angle to 4 quadrants/zones.
        // We use 45-degree thresholds to decide the "snap" point.

        // CASE 1: Standard Landscape (Roll is near 0)
        // Range: -45 to +45
        if (roll >= -45 && roll <= 45)
        {
            return 0; // No rotation needed
        }

        // CASE 2: Upside Down (Roll is near 180 or -180)
        // Range: > 135 or < -135
        else if (roll > 135 || roll < -135)
        {
            return 2; // Rotate 180 degrees (2 x 90)
        }

        // CASE 3: Banked Left / Portrait Left (Roll is near -90)
        // Range: -135 to -45
        else if (roll >= -135 && roll < -45)
        {
            return 1; // Rotate 90 degrees Clockwise
        }

        // CASE 4: Banked Right / Portrait Right (Roll is near +90)
        // Range: 45 to 135
        else
        {
            return 3; // Rotate 270 degrees Clockwise (3 x 90)
        }
    }

    //public static RotationResult GetSmartRotation(DroneOrientation orient)
    //{
    //    // 1. Safety Check
    //    if (!orient.Pitch.HasValue || !orient.Roll.HasValue || !orient.Yaw.HasValue)
    //        return new RotationResult { RotationSteps = 0, ModeDetected = "No Data" };

    //    // --- LOGIC SPLIT ---

    //    // CASE A: NADIR / STRAIGHT DOWN (Pitch is near -90)
    //    // Here, we ignore Roll. We use YAW to orient the map "North Up".
    //    if (orient.Pitch.Value < -80)
    //    {
    //        // To make North "Up", we counteract the compass heading.
    //        // Example: If Heading is 90 (East), we rotate -90 to make North Up.
    //        double heading = orient.Yaw.Value;

    //        // Normalize and Calculate Steps (Result: 0=N, 1=E, 2=S, 3=W)
    //        int steps = (int)Math.Round(heading / 90.0);

    //        // Invert for correction (Counter-rotate)
    //        // If facing East (1), we rotate -1 (which is 3 in mod 4 arithmetic)
    //        int correction = (4 - (steps % 4)) % 4;

    //        return new RotationResult { RotationSteps = correction, ModeDetected = "Map Mode (Nadir)" };
    //    }

    //    // CASE B: OBLIQUE / 45 DEGREES (Pitch is > -80)
    //    // Here, we ignore Yaw. We use ROLL to keep the horizon flat.
    //    // This handles "Landscape to Portrait" conversion if the camera was rolled 90°.
    //    else
    //    {
    //        double roll = orient.Roll.Value;

    //        // Normalize Roll (-180 to 180)
    //        if (roll > 180)
    //            roll -= 360;

    //        // Calculate Steps (0=Level, 1=Left Down, -1=Right Down, 2=UpsideDown)
    //        int steps = (int)Math.Round(roll / 90.0);

    //        // Invert for correction
    //        // If camera rolled +90 (Portrait Right), we rotate -90 (Left) to fix.
    //        int correction = (4 - (steps % 4)) % 4;

    //        return new RotationResult { RotationSteps = correction, ModeDetected = "Horizon Mode (Oblique)" };
    //    }
    //}

    public static RotationResult GetSmartRotation(DroneOrientation orient)
    {
        if (!orient.Pitch.HasValue || !orient.Roll.HasValue || !orient.Yaw.HasValue)
            return new RotationResult { RotationSteps = 0, ModeDetected = "No Data" };

        double pitch = orient.Pitch.Value;
        double roll = orient.Roll.Value;
        double yaw = orient.Yaw.Value;

        // CASE A: NADIR / MAP MODE (Looking Straight Down)
        if (pitch < -80)
        {
            // Make North point up
            int yawSteps = (int)Math.Round(yaw / 90.0) % 4;
            int correction = (4 - yawSteps) % 4;

            return new RotationResult { RotationSteps = correction, ModeDetected = "Map Mode (Nadir)" };
        }

        // CASE B: OBLIQUE / HORIZON MODE
        else
        {
            // Normalize roll to -180 to +180
            while (roll > 180)
                roll -= 360;
            while (roll <= -180)
                roll += 360;

            // FIXED: Correct mapping for portrait/landscape detection
            if (roll >= -45 && roll <= 45)
            {
                // Normal landscape orientation
                return new RotationResult { RotationSteps = 0, ModeDetected = "Horizon Mode (Landscape)" };
            }
            else if (roll > 135 || roll < -135)
            {
                // Upside down
                return new RotationResult { RotationSteps = 2, ModeDetected = "Horizon Mode (Inverted)" };
            }
            else if (roll > 45 && roll <= 135)
            {
                // RIGHT side down (Roll = +90°) → Rotate 90° CW to fix
                return new RotationResult { RotationSteps = 1, ModeDetected = "Horizon Mode (Portrait - Right Down)" };
            }
            else // roll < -45 && roll >= -135
            {
                // LEFT side down (Roll = -90°) → Rotate 270° CW (90° CCW) to fix
                return new RotationResult { RotationSteps = 3, ModeDetected = "Horizon Mode (Portrait - Left Down)" };
            }
        }
    }
}

// --- Usage Example ---
// class Program {
//     static void Main() {
//         var info = CameraOrientationReader.GetOrientation(@"C:\Photos\DJI_001.JPG");
//         Console.WriteLine(info.Description);
//     }
// }
