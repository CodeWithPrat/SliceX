using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SliceX.Models
{
    public class Model3D
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public MeshGeometry3D Geometry { get; set; }
        public Transform3D Transform { get; set; } = Transform3D.Identity;
        public List<Triangle> Triangles { get; set; } = new List<Triangle>();
        public Point3D Center { get; set; }
        public Size3D Size { get; set; }
        public Point3D OriginalCenter { get; set; }
        public Size3D OriginalSize { get; set; }
        
        // Transformation properties
        public Vector3D Position { get; set; } = new Vector3D(0, 0, 0);
        public Vector3D Rotation { get; set; } = new Vector3D(0, 0, 0);
        public Vector3D Scale { get; set; } = new Vector3D(1, 1, 1);
    }

    public class Triangle
    {
        public Point3D V1 { get; set; }
        public Point3D V2 { get; set; }
        public Point3D V3 { get; set; }
        public Vector3D Normal { get; set; }
    }
}