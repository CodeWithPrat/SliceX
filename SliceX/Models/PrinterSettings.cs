using System;

namespace SliceX.Models
{
    public class PrinterSettings
    {
        // Basic Profile
        public string ProfileName { get; set; } = "default";
        public string Notes { get; set; } = "";
        
        // Slicing Parameters
        public double LayerThickness { get; set; } = 0.025;
        public double ExposureTime { get; set; } = 1.0; // seconds
        public double BottomExposureTime { get; set; } = 5.0; // seconds
        public int BottomLayers { get; set; } = 3;
        
        // Build Volume
        public double BuildVolumeX { get; set; } = 14.515;
        public double BuildVolumeY { get; set; } = 8.165;
        public double BuildVolumeZ { get; set; } = 25.0;
        
        // Lift Settings
        public double LiftHeight { get; set; } = 5;
        public double LiftSpeed { get; set; } = 50; // mm/m
        public double RetractSpeed { get; set; } = 100; // mm/m
        public double LiftSequenceTime { get; set; } = 2.0; // seconds
        
        // Advanced Settings
        public bool EnableAntiAliasing { get; set; } = true;
        public bool ExportImagesAndGCode { get; set; } = true;
        public int ImageOffsetX { get; set; } = 0;
        public int ImageOffsetY { get; set; } = 0;
        public bool ReflectX { get; set; } = false;
        public bool ReflectY { get; set; } = false;
        public double ResinPricePerLiter { get; set; } = 0;
        public string BuildDirection { get; set; } = "Bottom_Up";
        public double SlideTiltValue { get; set; } = 0;
        
        // Movement Steps
        public double MoveStep { get; set; } = 10.0;
        public double RotateStep { get; set; } = 90.0;
        public double ScaleStep { get; set; } = 0.1;
    }
}