//ModelImporter.cs
using Assimp;
using SliceX.Models;
using System;
using System.Windows.Media.Media3D;
using Model3D = SliceX.Models.Model3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace SliceX.Utilities
{
    public class ModelImporter
    {
        private readonly AssimpContext importer = new AssimpContext();

        public Model3D ImportModel(string filePath)
        {
            try
            {
                var scene = importer.ImportFile(filePath, 
                    PostProcessSteps.Triangulate | 
                    PostProcessSteps.GenerateNormals |
                    PostProcessSteps.JoinIdenticalVertices);

                if (scene == null || !scene.HasMeshes)
                    throw new Exception("No valid mesh data found in file");

                var mesh = scene.Meshes[0];

                var model = new Model3D
                {
                    FilePath = filePath,
                    FileName = System.IO.Path.GetFileName(filePath),
                    Geometry = ConvertToMeshGeometry3D(mesh),
                    Triangles = ExtractTriangles(mesh)
                };

                CalculateBounds(model);
                
                // Store original bounds for reset functionality
                model.OriginalCenter = model.Center;
                model.OriginalSize = model.Size;
                
                return model;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to import model: {ex.Message}", ex);
            }
        }

        private MeshGeometry3D ConvertToMeshGeometry3D(Mesh mesh)
        {
            var geometry = new MeshGeometry3D();

            foreach (var vertex in mesh.Vertices)
            {
                geometry.Positions.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
            }

            foreach (var face in mesh.Faces)
            {
                if (face.IndexCount == 3)
                {
                    geometry.TriangleIndices.Add(face.Indices[0]);
                    geometry.TriangleIndices.Add(face.Indices[1]);
                    geometry.TriangleIndices.Add(face.Indices[2]);
                }
            }

            if (!mesh.HasNormals)
            {
                CalculateNormalsForGeometry(geometry);
            }
            else
            {
                foreach (var normal in mesh.Normals)
                {
                    geometry.Normals.Add(new Vector3D(normal.X, normal.Y, normal.Z));
                }
            }

            return geometry;
        }

        private void CalculateNormalsForGeometry(MeshGeometry3D geometry)
        {
            geometry.Normals.Clear();

            for (int i = 0; i < geometry.Positions.Count; i++)
            {
                geometry.Normals.Add(new Vector3D(0, 0, 0));
            }

            for (int i = 0; i < geometry.TriangleIndices.Count; i += 3)
            {
                int index1 = geometry.TriangleIndices[i];
                int index2 = geometry.TriangleIndices[i + 1];
                int index3 = geometry.TriangleIndices[i + 2];

                Point3D p1 = geometry.Positions[index1];
                Point3D p2 = geometry.Positions[index2];
                Point3D p3 = geometry.Positions[index3];

                Vector3D v1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
                Vector3D v2 = new Vector3D(p3.X - p1.X, p3.Y - p1.Y, p3.Z - p1.Z);
                
                Vector3D normal = Vector3D.CrossProduct(v1, v2);
                normal.Normalize();

                geometry.Normals[index1] += normal;
                geometry.Normals[index2] += normal;
                geometry.Normals[index3] += normal;
            }

            for (int i = 0; i < geometry.Normals.Count; i++)
            {
                geometry.Normals[i].Normalize();
            }
        }

        private List<Triangle> ExtractTriangles(Mesh mesh)
        {
            var triangles = new List<Triangle>();

            foreach (var face in mesh.Faces)
            {
                if (face.IndexCount == 3)
                {
                    var v1 = mesh.Vertices[face.Indices[0]];
                    var v2 = mesh.Vertices[face.Indices[1]];
                    var v3 = mesh.Vertices[face.Indices[2]];

                    triangles.Add(new Triangle
                    {
                        V1 = new Point3D(v1.X, v1.Y, v1.Z),
                        V2 = new Point3D(v2.X, v2.Y, v2.Z),
                        V3 = new Point3D(v3.X, v3.Y, v3.Z)
                    });
                }
            }

            return triangles;
        }

        private void CalculateBounds(Model3D model)
        {
            if (model.Geometry.Positions.Count == 0)
                return;

            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

            foreach (Point3D point in model.Geometry.Positions)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                minZ = Math.Min(minZ, point.Z);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
                maxZ = Math.Max(maxZ, point.Z);
            }

            model.Size = new Size3D(maxX - minX, maxY - minY, maxZ - minZ);
            model.Center = new Point3D((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
        }
    }
}