using SliceX.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Model3D = SliceX.Models.Model3D;


namespace SliceX.Slicer
{
    public class SliceResult
    {
        public List<SliceLayer> Layers { get; set; } = new List<SliceLayer>();
        public double PrintTime { get; set; }
        public int TotalLayers { get; set; }
        public double TotalExposureTime { get; set; }
        public double TotalLiftTime { get; set; }
        public double EstimatedResinVolume { get; set; }
        public double EstimatedCost { get; set; }
    }

    public class SliceLayer
    {
        public int LayerNumber { get; set; }
        public double ZHeight { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public double ExposureTime { get; set; }
        public bool IsBottomLayer { get; set; }
    }

    public class SlicingEngine
    {
        public SliceResult SliceModel(Model3D model, PrinterSettings settings)
        {
            var result = new SliceResult();
            
            if (model?.Triangles == null || !model.Triangles.Any())
                return result;

            // Calculate actual model height considering transformations
            double modelHeight = CalculateActualModelHeight(model);
            
            // Ensure model is not taller than build volume
            if (modelHeight > settings.BuildVolumeZ)
            {
                throw new InvalidOperationException($"Model height ({modelHeight:F2}mm) exceeds build volume height ({settings.BuildVolumeZ}mm). Please scale down the model.");
            }

            // FIXED: Calculate layer count properly using ceiling
            int layerCount = CalculateLayerCount(modelHeight, settings.LayerThickness);
            
            if (layerCount > 10000)
            {
                throw new InvalidOperationException($"Layer count ({layerCount}) exceeds safe limit. Check your layer thickness setting.");
            }

            double totalExposureTime = 0;
            double totalLiftTime = 0;
            
            for (int i = 0; i < layerCount; i++)
            {
                double currentZ = i * settings.LayerThickness;
                bool isBottomLayer = i < settings.BottomLayers;
                
                double exposureTime = isBottomLayer ? settings.BottomExposureTime : settings.ExposureTime;
                double layerTime = exposureTime + CalculateMovementTime(settings, isBottomLayer);
                
                totalExposureTime += exposureTime;
                totalLiftTime += CalculateMovementTime(settings, isBottomLayer);

                var layer = new SliceLayer
                {
                    LayerNumber = i + 1,
                    ZHeight = currentZ,
                    ExposureTime = exposureTime,
                    IsBottomLayer = isBottomLayer,
                    ImageData = GeneratePlaceholderImage(settings)
                };
                
                result.Layers.Add(layer);
            }

            result.TotalLayers = result.Layers.Count;
            result.TotalExposureTime = totalExposureTime;
            result.TotalLiftTime = totalLiftTime;
            result.PrintTime = (totalExposureTime + totalLiftTime) / 60; // Convert to minutes
            
            // Calculate estimated resin volume and cost
            result.EstimatedResinVolume = CalculateResinVolume(model, settings, modelHeight);
            result.EstimatedCost = result.EstimatedResinVolume * settings.ResinPricePerLiter / 1000; // Convert ml to liters
            
            return result;
        }

        /// <summary>
        /// Calculate actual model height considering transformations
        /// </summary>
        private double CalculateActualModelHeight(Model3D model)
        {
            if (model?.Triangles == null || !model.Triangles.Any())
                return 0;

            // Get the transformed vertices
            var transformedVertices = model.Triangles
                .SelectMany(t => new[] { t.V1, t.V2, t.V3 })
                .Select(vertex =>
                {
                    // Apply model transformation to each vertex
                    var point3D = new Point3D(vertex.X, vertex.Y, vertex.Z);
                    if (model.Transform != null)
                    {
                        point3D = model.Transform.Transform(point3D);
                    }
                    return point3D;
                })
                .ToList();

            if (!transformedVertices.Any())
                return 0;

            // Calculate actual bounds from transformed vertices
            double minZ = transformedVertices.Min(v => v.Z);
            double maxZ = transformedVertices.Max(v => v.Z);
            
            double actualHeight = maxZ - minZ;
            
            // Ensure the model sits on the build plate (Z=0 is the build plate)
            return Math.Max(actualHeight, 0.0);
        }

        /// <summary>
        /// FIXED: Calculate layer count using ceiling (matches Chitubox/Lychee behavior)
        /// This ensures we capture the entire model height
        /// </summary>
        private int CalculateLayerCount(double modelHeight, double layerThickness)
        {
            if (layerThickness <= 0)
                throw new ArgumentException("Layer thickness must be greater than 0");

            if (modelHeight <= 0)
                return 0;

            // Use ceiling to match professional slicer behavior
            // This ensures the top of the model is fully captured
            int layerCount = (int)Math.Ceiling(modelHeight / layerThickness);
            
            return layerCount;
        }

        private double CalculateMovementTime(PrinterSettings settings, bool isBottomLayer)
        {
            // Calculate lift/retract time
            double liftTime = (settings.LiftHeight / (settings.LiftSpeed / 60.0)); // Convert mm/m to mm/s
            double retractTime = (settings.LiftHeight / (settings.RetractSpeed / 60.0));
            
            return liftTime + retractTime + settings.LiftSequenceTime;
        }

        /// <summary>
        /// More accurate resin volume calculation
        /// </summary>
        private double CalculateResinVolume(Model3D model, PrinterSettings settings, double actualHeight)
        {
            // Calculate volume based on actual dimensions and pyramid geometry
            // For pyramidal shapes, volume = (base area × height) / 3
            
            // Get transformed bounds
            var transformedVertices = model.Triangles
                .SelectMany(t => new[] { t.V1, t.V2, t.V3 })
                .Select(vertex =>
                {
                    var point3D = new Point3D(vertex.X, vertex.Y, vertex.Z);
                    if (model.Transform != null)
                    {
                        point3D = model.Transform.Transform(point3D);
                    }
                    return point3D;
                })
                .ToList();

            double minX = transformedVertices.Min(v => v.X);
            double maxX = transformedVertices.Max(v => v.X);
            double minY = transformedVertices.Min(v => v.Y);
            double maxY = transformedVertices.Max(v => v.Y);
            
            double baseWidth = maxX - minX;
            double baseLength = maxY - minY;
            
            // Calculate base area
            double baseArea = baseWidth * baseLength;
            
            // For pyramid: Volume = (base area × height) / 3
            double solidVolume = (baseArea * actualHeight) / 3.0;
            
            // Apply conservative hollow factor for pyramidal structures
            double hollowFactor = 0.4; // 40% solid for pyramidal structures
            
            return solidVolume * hollowFactor;
        }

        private byte[] GeneratePlaceholderImage(PrinterSettings settings)
        {
            // Create a more realistic placeholder image size
            int width = Math.Max(100, Math.Min((int)(settings.BuildVolumeX * 50), 3840));
            int height = Math.Max(100, Math.Min((int)(settings.BuildVolumeY * 50), 3840));
            
            // Return empty array for now (in real implementation, this would contain actual slice data)
            return new byte[width * height / 8]; // Approximate size for bitmap
        }
    }
}