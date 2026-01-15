# Drone Image Orientation Detection & Auto-Rotation

Intelligent camera gimbal metadata analysis for optimal image display. 

## Overview

This C# application automatically detects the orientation of drone-captured images by reading embedded XMP metadata from the camera gimbal (with EXIF GPS as a fallback). It determines how many 90° clockwise rotations are needed so images display upright and consistently, whether they were captured in landscape, portrait, nadir (straight down), or inverted positions. 

## Why this exists

Drone photo sets often contain sideways or upside-down images due to: 

– Intentional portrait shooting (gimbal rotated 90°)
– Nadir photography for mapping (camera pointing straight down)
– Banked flight maneuvers or extreme gimbal angles
– Inverted positions

Manually rotating hundreds or thousands of images is slow and inconsistent.

## Key features

– Dual-mode detection based on camera pitch angle 
– Map Mode (nadir): when pitch < -80°, use yaw/heading to orient North up 
– Horizon Mode (oblique): when pitch > -80°, use roll to level horizon and detect portrait/banking 
– Portrait mode recognition with discrete 90° steps (0–3) 
– Multi-brand friendly design (DJI XMP schema now, expandable to other XMP formats) 

## How it works

### Metadata inputs

– Pitch: camera tilt angle (+90° up, -90° down) to choose Map vs Horizon mode 
– Yaw: compass heading (0–360°) used in Map Mode to orient North upward 
– Roll: gimbal roll angle used in Horizon Mode to detect portrait orientation and banking 

### Rotation output

The app returns:

– `RotationSteps`: integer 0–3 (number of 90° clockwise rotations) 
– `ModeDetected`: string (Map Mode or Horizon Mode) 
– Optional human-readable description of detected orientation 

### Rotation logic (Horizon Mode)

Roll angle range to rotation mapping: 

– -45° to +45° → 0 steps (no rotation)
– +45° to +135° → 1 step (90° CW)
– > +135° or < -135° → 2 steps (180°)
– -45° to -135° → 3 steps (270° CW / 90° CCW)

## Use cases

– Photo management: auto-rotate on import or batch processing 
– Mapping: keep nadir shots consistently North-up for orthomosaic workflows 
– Real estate: mixed landscape and portrait sets without manual rotation 
– Inspection & surveying: standard orientation across large datasets 
– Web galleries: pre-process images for correct display on all devices 

## Dependencies

– .NET / C# runtime 
– `MetadataExtractor` library for reading EXIF and XMP data 

## Quick start

### 1) Build

– Open the solution in Visual Studio (or use `dotnet build`)
– Restore NuGet packages (ensure `MetadataExtractor` is installed)

### 2) Run

– Point the app at a folder of images (or a single image)
– For each image, read XMP/EXIF metadata and compute `RotationSteps`
– Apply the rotation in your viewer, exporter, or batch pipeline (rotate by `RotationSteps * 90°` CW)

> Note: This repo focuses on computing rotation steps. Applying rotation can be done via your preferred imaging library (ImageSharp, System.Drawing, SkiaSharp, OpenCV, etc.).

## Recommended project structure

– `src/`
– `DroneImageOrientation/` (core library: metadata read + rotation logic)
– `DroneImageOrientation.Cli/` (optional CLI wrapper)
– `tests/` (unit tests for roll/pitch edge cases and sample metadata files)

## Roadmap

– Support more drone manufacturers (Autel, Skydio, Parrot) 
– Configurable pitch threshold for Map vs Horizon mode 
– Batch processing API for large collections 
– Optional integration to auto-apply rotation to files 
– ML fallback for images without usable metadata 

## Contributing

– Issues and PRs are welcome
– Please include sample metadata (sanitized) and expected `RotationSteps` for new edge cases
– Add tests for any bug fix or new rule

## License

– Add your preferred license (MIT, Apache-2.0, or proprietary) in `LICENSE`
