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
        /// <summary>
        /// Generate a black and white bitmap image for a specific layer
        /// </summary>
        public BitmapSource GenerateLayerImage(Model3D model, double zHeight, PrinterSettings settings)
        {
            // Calculate image dimensions based on build volume and resolution
            int imageWidth = (int)(settings.BuildVolumeX * 10); // 10 pixels per mm
            int imageHeight = (int)(settings.BuildVolumeY * 10);
            
            // Ensure reasonable image size
            imageWidth = Math.Min(Math.Max(imageWidth, 100), 3840);
            imageHeight = Math.Min(Math.Max(imageHeight, 100), 2160);
            
            // Create pixel buffer
            byte[] pixels = new byte[imageWidth * imageHeight * 3]; // BGR24 format
            
            // Initialize to black (0 = no exposure)
            Array.Clear(pixels, 0, pixels.Length);
            
            // Get intersections at this Z height
            var intersections = GetLayerIntersections(model, zHeight, settings);
            
            // Draw lines
            foreach (var line in intersections)
            {
                DrawLine(pixels, imageWidth, imageHeight, line.Start, line.End);
            }
            
            // Fill polygons
            FillPolygons(pixels, imageWidth, imageHeight, intersections);
            
            // Create bitmap from pixel buffer
            var bitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Bgr24, null);
            bitmap.WritePixels(new Int32Rect(0, 0, imageWidth, imageHeight), pixels, imageWidth * 3, 0);
            
            return bitmap;
        }

        /// <summary>
        /// Convert BitmapSource to byte array (PNG format)
        /// </summary>
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
        
        private class LineSegment2D
        {
            public Point Start { get; set; }
            public Point End { get; set; }
        }
        
        private List<LineSegment2D> GetLayerIntersections(Model3D model, double zHeight, PrinterSettings settings)
        {
            var lines = new List<LineSegment2D>();
            
            if (model?.Triangles == null || !model.Triangles.Any())
                return lines;
            
            double tolerance = 0.001; // 1 micron tolerance
            
            foreach (var triangle in model.Triangles)
            {
                // Transform vertices
                var v1 = TransformVertex(triangle.V1, model.Transform);
                var v2 = TransformVertex(triangle.V2, model.Transform);
                var v3 = TransformVertex(triangle.V3, model.Transform);
                
                // Check if triangle intersects this Z plane
                var vertices = new[] { v1, v2, v3 };
                var intersectionPoints = new List<Point>();
                
                // Check each edge for intersection with Z plane
                for (int i = 0; i < 3; i++)
                {
                    var p1 = vertices[i];
                    var p2 = vertices[(i + 1) % 3];
                    
                    // Check if edge crosses the Z plane
                    if ((p1.Z <= zHeight + tolerance && p2.Z >= zHeight - tolerance) ||
                        (p1.Z >= zHeight - tolerance && p2.Z <= zHeight + tolerance))
                    {
                        if (Math.Abs(p1.Z - p2.Z) < tolerance)
                        {
                            // Edge is on the plane
                            intersectionPoints.Add(WorldToPixel(p1, settings));
                            intersectionPoints.Add(WorldToPixel(p2, settings));
                        }
                        else
                        {
                            // Calculate intersection point
                            double t = (zHeight - p1.Z) / (p2.Z - p1.Z);
                            double x = p1.X + t * (p2.X - p1.X);
                            double y = p1.Y + t * (p2.Y - p1.Y);
                            
                            intersectionPoints.Add(WorldToPixel(new Point3D(x, y, zHeight), settings));
                        }
                    }
                }
                
                // Create line segments from intersection points
                if (intersectionPoints.Count >= 2)
                {
                    for (int i = 0; i < intersectionPoints.Count - 1; i += 2)
                    {
                        if (i + 1 < intersectionPoints.Count)
                        {
                            lines.Add(new LineSegment2D
                            {
                                Start = intersectionPoints[i],
                                End = intersectionPoints[i + 1]
                            });
                        }
                    }
                }
            }
            
            return lines;
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
            // Center of build volume is at (0, 0)
            double pixelX = (worldPoint.X + settings.BuildVolumeX / 2) * 10; // 10 pixels per mm
            double pixelY = (worldPoint.Y + settings.BuildVolumeY / 2) * 10;
            
            // Clamp to image bounds
            pixelX = Math.Max(0, Math.Min(pixelX, settings.BuildVolumeX * 10 - 1));
            pixelY = Math.Max(0, Math.Min(pixelY, settings.BuildVolumeY * 10 - 1));
            
            return new Point(pixelX, pixelY);
        }
        
        private void DrawLine(byte[] pixels, int width, int height, Point p1, Point p2)
        {
            // Bresenham's line algorithm
            int x0 = (int)p1.X;
            int y0 = (int)p1.Y;
            int x1 = (int)p2.X;
            int y1 = (int)p2.Y;
            
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            
            while (true)
            {
                // Draw pixel (white = exposed)
                if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
                {
                    int index = (y0 * width + x0) * 3;
                    pixels[index] = 255;     // B
                    pixels[index + 1] = 255; // G
                    pixels[index + 2] = 255; // R
                }
                
                if (x0 == x1 && y0 == y1) break;
                
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
        
        private void FillPolygons(byte[] pixels, int width, int height, List<LineSegment2D> lines)
        {
            // Simple scanline fill algorithm
            for (int y = 0; y < height; y++)
            {
                var intersections = new List<int>();
                
                // Find all X intersections for this scanline
                foreach (var line in lines)
                {
                    double y1 = line.Start.Y;
                    double y2 = line.End.Y;
                    
                    if ((y1 <= y && y2 >= y) || (y2 <= y && y1 >= y))
                    {
                        double x1 = line.Start.X;
                        double x2 = line.End.X;
                        
                        if (Math.Abs(y2 - y1) < 0.001)
                        {
                            intersections.Add((int)x1);
                            intersections.Add((int)x2);
                        }
                        else
                        {
                            double t = (y - y1) / (y2 - y1);
                            int x = (int)(x1 + t * (x2 - x1));
                            intersections.Add(x);
                        }
                    }
                }
                
                // Sort intersections
                intersections.Sort();
                
                // Fill between pairs
                for (int i = 0; i < intersections.Count - 1; i += 2)
                {
                    int x1 = Math.Max(0, intersections[i]);
                    int x2 = Math.Min(width - 1, intersections[i + 1]);
                    
                    for (int x = x1; x <= x2; x++)
                    {
                        int index = (y * width + x) * 3;
                        pixels[index] = 255;     // B
                        pixels[index + 1] = 255; // G
                        pixels[index + 2] = 255; // R
                    }
                }
            }
        }
    }
}