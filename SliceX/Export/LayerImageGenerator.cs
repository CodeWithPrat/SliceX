using SliceX.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Model3D = SliceX.Models.Model3D;

namespace SliceX.Export
{
    public class LayerImageGenerator
    {
        private const double PIXELS_PER_MM = 3.7795275591; // 96 DPI = 96/25.4 pixels per mm
        
        public BitmapSource GenerateLayerImage(Model3D model, double zHeight, PrinterSettings settings)
        {
            // Calculate dimensions based on build volume
            int imageWidth = (int)(settings.BuildVolumeX * PIXELS_PER_MM);
            int imageHeight = (int)(settings.BuildVolumeY * PIXELS_PER_MM);
            
            // Ensure dimensions match exactly 1920x1080 as specified
            imageWidth = 1920;
            imageHeight = 1080;
            
            // Create pixel buffer with white background (32-bit ARGB format)
            byte[] pixels = new byte[imageWidth * imageHeight * 4]; // BGRA32 format for 32-bit depth
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;     // B
                pixels[i + 1] = 255; // G
                pixels[i + 2] = 255; // R
                pixels[i + 3] = 255; // A (fully opaque)
            }
            
            // Get layer contours at this Z height
            var contours = GetLayerContours(model, zHeight, settings);
            
            if (contours.Any())
            {
                // Fill polygons first
                FillContours(pixels, imageWidth, imageHeight, contours);
                
                // Then draw outlines
                foreach (var contour in contours)
                {
                    DrawContour(pixels, imageWidth, imageHeight, contour);
                }
            }
            
            // Create bitmap with exact specifications from the image
            var bitmap = new WriteableBitmap(
                imageWidth, 
                imageHeight, 
                96,  // Horizontal resolution (DPI)
                96,  // Vertical resolution (DPI)
                PixelFormats.Bgra32, // 32-bit format
                null);
                
            bitmap.WritePixels(
                new Int32Rect(0, 0, imageWidth, imageHeight), 
                pixels, 
                imageWidth * 4, // Stride for 32-bit (4 bytes per pixel)
                0);
            
            return bitmap;
        }

        public byte[] BitmapToByteArray(BitmapSource bitmap)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }
        
        private class Contour
        {
            public List<Point> Points { get; set; } = new List<Point>();
            public bool IsHole { get; set; }
        }
        
        private List<Contour> GetLayerContours(Model3D model, double zHeight, PrinterSettings settings)
        {
            var contours = new List<Contour>();
            
            if (model?.Triangles == null || !model.Triangles.Any())
                return contours;
            
            double tolerance = 0.001; // 1 micron tolerance
            var intersectionSegments = new List<LineSegment2D>();
            
            foreach (var triangle in model.Triangles)
            {
                var v1 = TransformVertex(triangle.V1, model.Transform);
                var v2 = TransformVertex(triangle.V2, model.Transform);
                var v3 = TransformVertex(triangle.V3, model.Transform);
                
                var vertices = new[] { v1, v2, v3 };
                var intersectionPoints = new List<Point3D>();
                
                // Check each edge for intersection with Z plane
                for (int i = 0; i < 3; i++)
                {
                    var p1 = vertices[i];
                    var p2 = vertices[(i + 1) % 3];
                    
                    // Edge crosses the Z plane
                    if ((p1.Z < zHeight && p2.Z > zHeight) || (p1.Z > zHeight && p2.Z < zHeight))
                    {
                        double t = (zHeight - p1.Z) / (p2.Z - p1.Z);
                        double x = p1.X + t * (p2.X - p1.X);
                        double y = p1.Y + t * (p2.Y - p1.Y);
                        
                        intersectionPoints.Add(new Point3D(x, y, zHeight));
                    }
                    // Edge lies exactly on the Z plane
                    else if (Math.Abs(p1.Z - zHeight) < tolerance && Math.Abs(p2.Z - zHeight) < tolerance)
                    {
                        intersectionPoints.Add(p1);
                        intersectionPoints.Add(p2);
                    }
                }
                
                // Remove duplicates and create segments
                var uniquePoints = new List<Point3D>();
                foreach (var point in intersectionPoints)
                {
                    if (!uniquePoints.Any(p => 
                        Math.Abs(p.X - point.X) < tolerance && 
                        Math.Abs(p.Y - point.Y) < tolerance))
                    {
                        uniquePoints.Add(point);
                    }
                }
                
                // Create line segments from intersection points
                if (uniquePoints.Count == 2)
                {
                    var p1 = WorldToPixel(uniquePoints[0], settings);
                    var p2 = WorldToPixel(uniquePoints[1], settings);
                    
                    intersectionSegments.Add(new LineSegment2D { Start = p1, End = p2 });
                }
            }
            
            // Build contours from segments
            contours = BuildContoursFromSegments(intersectionSegments);
            
            return contours;
        }
        
        private List<Contour> BuildContoursFromSegments(List<LineSegment2D> segments)
        {
            var contours = new List<Contour>();
            var usedSegments = new HashSet<LineSegment2D>();
            
            foreach (var segment in segments)
            {
                if (usedSegments.Contains(segment))
                    continue;
                    
                var contour = new Contour();
                var currentSegment = segment;
                
                // Start building contour
                contour.Points.Add(currentSegment.Start);
                contour.Points.Add(currentSegment.End);
                usedSegments.Add(currentSegment);
                
                bool foundNext;
                do
                {
                    foundNext = false;
                    var lastPoint = contour.Points[contour.Points.Count - 1];
                    
                    // Find connected segment
                    foreach (var nextSegment in segments)
                    {
                        if (usedSegments.Contains(nextSegment))
                            continue;
                            
                        if (PointsAreEqual(lastPoint, nextSegment.Start, 0.5))
                        {
                            contour.Points.Add(nextSegment.End);
                            usedSegments.Add(nextSegment);
                            foundNext = true;
                            break;
                        }
                        else if (PointsAreEqual(lastPoint, nextSegment.End, 0.5))
                        {
                            contour.Points.Add(nextSegment.Start);
                            usedSegments.Add(nextSegment);
                            foundNext = true;
                            break;
                        }
                    }
                } while (foundNext);
                
                // Close the contour if needed
                if (contour.Points.Count > 2 && 
                    PointsAreEqual(contour.Points[0], contour.Points[contour.Points.Count - 1], 0.5))
                {
                    contour.Points.RemoveAt(contour.Points.Count - 1);
                }
                
                if (contour.Points.Count >= 3)
                {
                    contours.Add(contour);
                }
            }
            
            return contours;
        }
        
        private bool PointsAreEqual(Point p1, Point p2, double tolerance)
        {
            return Math.Abs(p1.X - p2.X) < tolerance && Math.Abs(p1.Y - p2.Y) < tolerance;
        }
        
        private Point3D TransformVertex(Point3D vertex, Transform3D transform)
        {
            var point = new Point3D(vertex.X, vertex.Y, vertex.Z);
            if (transform != null && transform != Transform3D.Identity)
            {
                point = transform.Transform(point);
            }
            return point;
        }
        
        private Point WorldToPixel(Point3D worldPoint, PrinterSettings settings)
        {
            // Convert world coordinates to pixel coordinates
            // Scale to fit within 1920x1080 while maintaining aspect ratio
            double scaleX = 1920 / settings.BuildVolumeX;
            double scaleY = 1080 / settings.BuildVolumeY;
            double scale = Math.Min(scaleX, scaleY) * 0.9; // 90% scale to add some margin
            
            // Center the model
            double pixelX = (worldPoint.X * scale) + (1920 / 2);
            double pixelY = (1080 / 2) - (worldPoint.Y * scale);
            
            return new Point(pixelX, pixelY);
        }
        
        private void FillContours(byte[] pixels, int width, int height, List<Contour> contours)
        {
            // Use even-odd fill rule
            for (int y = 0; y < height; y++)
            {
                var intersections = new List<double>();
                
                foreach (var contour in contours)
                {
                    for (int i = 0; i < contour.Points.Count; i++)
                    {
                        var p1 = contour.Points[i];
                        var p2 = contour.Points[(i + 1) % contour.Points.Count];
                        
                        if ((p1.Y <= y && p2.Y >= y) || (p2.Y <= y && p1.Y >= y))
                        {
                            if (Math.Abs(p2.Y - p1.Y) > 0.0001)
                            {
                                double t = (y - p1.Y) / (p2.Y - p1.Y);
                                double x = p1.X + t * (p2.X - p1.X);
                                intersections.Add(x);
                            }
                        }
                    }
                }
                
                // Sort and fill between pairs
                intersections.Sort();
                
                for (int i = 0; i < intersections.Count - 1; i += 2)
                {
                    int startX = Math.Max(0, (int)intersections[i]);
                    int endX = Math.Min(width - 1, (int)intersections[i + 1]);
                    
                    for (int x = startX; x <= endX; x++)
                    {
                        int index = (y * width + x) * 4;
                        // Black fill with full opacity
                        pixels[index] = 0;     // B
                        pixels[index + 1] = 0; // G
                        pixels[index + 2] = 0; // R
                        pixels[index + 3] = 255; // A
                    }
                }
            }
        }
        
        private void DrawContour(byte[] pixels, int width, int height, Contour contour)
        {
            for (int i = 0; i < contour.Points.Count; i++)
            {
                var start = contour.Points[i];
                var end = contour.Points[(i + 1) % contour.Points.Count];
                
                DrawLine(pixels, width, height, start, end);
            }
        }
        
        private void DrawLine(byte[] pixels, int width, int height, Point p1, Point p2)
        {
            int x0 = (int)Math.Round(p1.X);
            int y0 = (int)Math.Round(p1.Y);
            int x1 = (int)Math.Round(p2.X);
            int y1 = (int)Math.Round(p2.Y);
            
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
                {
                    int index = (y0 * width + x0) * 4;
                    // Black outline with full opacity
                    pixels[index] = 0;     // B
                    pixels[index + 1] = 0; // G
                    pixels[index + 2] = 0; // R
                    pixels[index + 3] = 255; // A
                }
                
                if (x0 == x1 && y0 == y1) break;
                
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }
        
        private class LineSegment2D
        {
            public Point Start { get; set; }
            public Point End { get; set; }
        }
    }
}