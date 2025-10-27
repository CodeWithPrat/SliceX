using SliceX.Models;
using SliceX.Slicer;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SliceX.Export
{
    public class SliceExporter
    {
        private readonly GCodeGenerator gcodeGenerator;
        private readonly LayerImageGenerator imageGenerator;
        
        public SliceExporter()
        {
            gcodeGenerator = new GCodeGenerator();
            imageGenerator = new LayerImageGenerator();
        }
        
        /// <summary>
        /// Export slicing result to a ZIP file containing G-code and layer images
        /// </summary>
        public void ExportToZip(Model3D model, SliceResult sliceResult, PrinterSettings settings, 
                                string outputPath, IProgress<int> progress = null)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            
            if (sliceResult == null)
                throw new ArgumentNullException(nameof(sliceResult));
            
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            
            // Create temporary directory for files
            string tempDir = Path.Combine(Path.GetTempPath(), $"SliceX_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Generate G-code (thread-safe)
                progress?.Report(0);
                string gcode = gcodeGenerator.GenerateGCode(sliceResult, settings);
                string gcodeFile = Path.Combine(tempDir, "output.gcode");
                File.WriteAllText(gcodeFile, gcode, Encoding.UTF8);
                
                // Create images directory
                string imagesDir = Path.Combine(tempDir, "layers");
                Directory.CreateDirectory(imagesDir);
                
                // Generate layer images
                int totalLayers = sliceResult.Layers.Count;
                for (int i = 0; i < totalLayers; i++)
                {
                    var layer = sliceResult.Layers[i];
                    
                    // Generate image for this layer on UI thread
                    byte[] imageBytes = null;
                    
                    // Use Dispatcher to create bitmap on UI thread
                    var dispatcher = System.Windows.Application.Current?.Dispatcher;
                    if (dispatcher != null && !dispatcher.CheckAccess())
                    {
                        dispatcher.Invoke(() =>
                        {
                            var bitmap = imageGenerator.GenerateLayerImage(model, layer.ZHeight, settings);
                            // Freeze the bitmap so it can be accessed from other threads
                            bitmap.Freeze();
                            imageBytes = imageGenerator.BitmapToByteArray(bitmap);
                        });
                    }
                    else
                    {
                        var bitmap = imageGenerator.GenerateLayerImage(model, layer.ZHeight, settings);
                        bitmap.Freeze();
                        imageBytes = imageGenerator.BitmapToByteArray(bitmap);
                    }
                    
                    // Save as PNG
                    string imagePath = Path.Combine(imagesDir, $"layer_{layer.LayerNumber:D5}.png");
                    File.WriteAllBytes(imagePath, imageBytes);
                    
                    // Report progress
                    int progressPercent = (int)((i + 1) * 100.0 / totalLayers);
                    progress?.Report(progressPercent);
                }
                
                // Create metadata file
                string metadataFile = Path.Combine(tempDir, "metadata.txt");
                CreateMetadataFile(metadataFile, sliceResult, settings);
                
                // Create ZIP file
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                
                ZipFile.CreateFromDirectory(tempDir, outputPath, CompressionLevel.Optimal, false);
                
                progress?.Report(100);
            }
            finally
            {
                // Clean up temporary directory
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        
        private void SaveBitmapAsPng(BitmapSource bitmap, string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(fileStream);
            }
        }
        
        private void CreateMetadataFile(string path, SliceResult sliceResult, PrinterSettings settings)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("SliceX Export Metadata");
            sb.AppendLine("======================");
            sb.AppendLine();
            sb.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Profile Name: {settings.ProfileName}");
            sb.AppendLine();
            
            sb.AppendLine("Print Statistics:");
            sb.AppendLine($"  Total Layers: {sliceResult.TotalLayers}");
            sb.AppendLine($"  Print Time: {sliceResult.PrintTime:F1} minutes ({sliceResult.PrintTime / 60:F1} hours)");
            sb.AppendLine($"  Total Exposure Time: {sliceResult.TotalExposureTime:F0} seconds");
            sb.AppendLine($"  Total Lift Time: {sliceResult.TotalLiftTime:F0} seconds");
            sb.AppendLine($"  Estimated Resin Volume: {sliceResult.EstimatedResinVolume:F1} ml");
            sb.AppendLine($"  Estimated Cost: ${sliceResult.EstimatedCost:F2}");
            sb.AppendLine();
            
            sb.AppendLine("Printer Settings:");
            sb.AppendLine($"  Build Volume: {settings.BuildVolumeX} x {settings.BuildVolumeY} x {settings.BuildVolumeZ} mm");
            sb.AppendLine($"  Layer Thickness: {settings.LayerThickness} mm");
            sb.AppendLine($"  Exposure Time: {settings.ExposureTime} s");
            sb.AppendLine($"  Bottom Exposure Time: {settings.BottomExposureTime} s");
            sb.AppendLine($"  Bottom Layers: {settings.BottomLayers}");
            sb.AppendLine($"  Lift Height: {settings.LiftHeight} mm");
            sb.AppendLine($"  Lift Speed: {settings.LiftSpeed} mm/min");
            sb.AppendLine($"  Retract Speed: {settings.RetractSpeed} mm/min");
            sb.AppendLine($"  Lift Sequence Time: {settings.LiftSequenceTime} s");
            sb.AppendLine($"  Anti-Aliasing: {settings.EnableAntiAliasing}");
            sb.AppendLine($"  Resin Price: ${settings.ResinPricePerLiter} per liter");
            sb.AppendLine();
            
            sb.AppendLine("Layer Information:");
            sb.AppendLine($"  First Layer Z: {sliceResult.Layers[0].ZHeight:F3} mm");
            sb.AppendLine($"  Last Layer Z: {sliceResult.Layers[sliceResult.Layers.Count - 1].ZHeight:F3} mm");
            sb.AppendLine();
            
            sb.AppendLine("Files:");
            sb.AppendLine("  output.gcode - G-code for the print");
            sb.AppendLine($"  layers/ - {sliceResult.TotalLayers} PNG images (layer_00001.png to layer_{sliceResult.TotalLayers:D5}.png)");
            
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }
}