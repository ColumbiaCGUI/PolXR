using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

namespace OptimizedOBJLoader
{
    public enum SplitMode
    {
        None,      // Single mesh for the entire object
        Object,    // Split by object/group
        Material   // Split by material
    }

    public class OBJLoader
    {
        public SplitMode SplitMode = SplitMode.Object;
        private FileInfo _objInfo;
        private Dictionary<string, Material> Materials;
        private const int MaxVerticesPerMesh = 65000; // Unity's vertex limit per mesh

        // Temporary data structures for parsing
        private List<Vector3> vertices = new List<Vector3>();
        private List<Vector3> normals = new List<Vector3>();
        private List<Vector2> uvs = new List<Vector2>();

        public GameObject Load(string objPath)
        {
            try
            {
                _objInfo = new FileInfo(objPath);
                if (!_objInfo.Exists)
                {
                    Debug.LogError($"OBJ file not found: {objPath}");
                    return null;
                }

                return LoadInternal(objPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load OBJ: {objPath}. Error: {ex.Message}");
                return null;
            }
        }

        private GameObject LoadInternal(string objPath)
        {
            Dictionary<string, ObjectBuilder> builders = new Dictionary<string, ObjectBuilder>();
            ObjectBuilder currentBuilder = null;
            string currentMaterial = "default";
            string currentObjectName = "default";

            // Initialize default builder
            builders[currentObjectName] = new ObjectBuilder(currentObjectName, this);

            using (var stream = new FileStream(objPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;

                    var parts = Regex.Split(line, @"\s+");
                    if (parts.Length == 0)
                        continue;

                    switch (parts[0])
                    {
                        case "v":
                            if (parts.Length >= 4)
                                vertices.Add(ParseVector3(parts));
                            break;
                        case "vn":
                            if (parts.Length >= 4)
                                normals.Add(ParseVector3(parts));
                            break;
                        case "vt":
                            if (parts.Length >= 3)
                                uvs.Add(ParseVector2(parts));
                            break;
                        case "mtllib":
                            if (parts.Length >= 2)
                                LoadMaterialLibrary(parts[1]);
                            break;
                        case "usemtl":
                            if (parts.Length >= 2)
                            {
                                currentMaterial = parts[1];
                                if (SplitMode == SplitMode.Material)
                                {
                                    currentObjectName = currentMaterial;
                                    if (!builders.ContainsKey(currentObjectName))
                                        builders[currentObjectName] = new ObjectBuilder(currentObjectName, this);
                                }
                            }
                            break;
                        case "o":
                        case "g":
                            if (SplitMode == SplitMode.Object && parts.Length >= 2)
                            {
                                currentObjectName = parts[1];
                                if (!builders.ContainsKey(currentObjectName))
                                    builders[currentObjectName] = new ObjectBuilder(currentObjectName, this);
                            }
                            break;
                        case "f":
                            currentBuilder = builders[currentObjectName];
                            ParseFace(parts, currentBuilder, currentMaterial);
                            break;
                    }
                }
            }

            // Build the final GameObject
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(_objInfo.Name));
            root.transform.localScale = new Vector3(-1f, 1f, 1f); // Adjust for Unity's coordinate system

            foreach (var builder in builders.Values)
            {
                if (builder.FaceCount == 0)
                    continue;

                var subObjects = builder.Build();
                foreach (var subObj in subObjects)
                {
                    subObj.transform.SetParent(root.transform, false);
                }
            }

            // Clear temporary data
            vertices.Clear();
            normals.Clear();
            uvs.Clear();

            return root;
        }

        private Vector3 ParseVector3(string[] parts)
        {
            float x = float.Parse(parts[1]);
            float y = float.Parse(parts[2]);
            float z = parts.Length > 3 ? float.Parse(parts[3]) : 0f;
            return new Vector3(x, y, z);
        }

        private Vector2 ParseVector2(string[] parts)
        {
            float u = float.Parse(parts[1]);
            float v = parts.Length > 2 ? float.Parse(parts[2]) : 0f;
            return new Vector2(u, v);
        }

        private void ParseFace(string[] parts, ObjectBuilder builder, string material)
        {
            List<int> vertexIndices = new List<int>();
            List<int> normalIndices = new List<int>();
            List<int> uvIndices = new List<int>();

            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;

                var indices = parts[i].Split('/');
                int vIdx = int.Parse(indices[0]);
                int uvIdx = indices.Length > 1 && !string.IsNullOrEmpty(indices[1]) ? int.Parse(indices[1]) : -1;
                int nIdx = indices.Length > 2 && !string.IsNullOrEmpty(indices[2]) ? int.Parse(indices[2]) : -1;

                // Handle negative indices
                if (vIdx < 0) vIdx = vertices.Count + vIdx;
                else vIdx--;
                if (uvIdx > 0) uvIdx--;
                if (nIdx > 0) nIdx--;

                vertexIndices.Add(vIdx);
                uvIndices.Add(uvIdx);
                normalIndices.Add(nIdx);
            }

            builder.AddFace(material, vertexIndices, normalIndices, uvIndices);
        }

        private void LoadMaterialLibrary(string mtlLibPath)
        {
            string mtlPath = Path.Combine(_objInfo.Directory.FullName, mtlLibPath);
            if (File.Exists(mtlPath))
            {
                Materials = new MTLLoader().Load(mtlPath);
            }
        }

        private class ObjectBuilder
        {
            private string name;
            private OBJLoader loader;
            private Dictionary<string, List<Face>> materialFaces = new Dictionary<string, List<Face>>();
            public int FaceCount { get; private set; }

            private struct Face
            {
                public List<int> VertexIndices;
                public List<int> NormalIndices;
                public List<int> UVIndices;
            }

            public ObjectBuilder(string name, OBJLoader loader)
            {
                this.name = name;
                this.loader = loader;
                FaceCount = 0;
            }

            public void AddFace(string material, List<int> vertexIndices, List<int> normalIndices, List<int> uvIndices)
            {
                if (!materialFaces.ContainsKey(material))
                    materialFaces[material] = new List<Face>();

                materialFaces[material].Add(new Face
                {
                    VertexIndices = new List<int>(vertexIndices),
                    NormalIndices = new List<int>(normalIndices),
                    UVIndices = new List<int>(uvIndices)
                });
                FaceCount++;
            }

            public List<GameObject> Build()
            {
                List<GameObject> subObjects = new List<GameObject>();
                int meshIndex = 0;

                foreach (var materialGroup in materialFaces)
                {
                    string materialName = materialGroup.Key;
                    var faces = materialGroup.Value;

                    // Split faces into meshes to respect vertex limit
                    List<Mesh> meshes = new List<Mesh>();
                    List<int> currentVertices = new List<int>();
                    List<int> currentTriangles = new List<int>();
                    Dictionary<int, int> vertexMap = new Dictionary<int, int>(); // Maps global vertex index to local mesh index
                    List<Vector3> meshVertices = new List<Vector3>();
                    List<Vector3> meshNormals = new List<Vector3>();
                    List<Vector2> meshUVs = new List<Vector2>();
                    int localVertexCount = 0;

                    foreach (var face in faces)
                    {
                        // Check if adding this face exceeds vertex limit
                        var uniqueVertices = face.VertexIndices.Distinct().ToList();
                        if (localVertexCount + uniqueVertices.Count > MaxVerticesPerMesh)
                        {
                            // Create a new mesh
                            meshes.Add(CreateMesh(meshVertices, meshNormals, meshUVs, currentTriangles, materialName));
                            meshVertices.Clear();
                            meshNormals.Clear();
                            meshUVs.Clear();
                            currentTriangles.Clear();
                            vertexMap.Clear();
                            localVertexCount = 0;
                        }

                        // Process face vertices
                        for (int i = 0; i < face.VertexIndices.Count; i++)
                        {
                            int vIdx = face.VertexIndices[i];
                            if (!vertexMap.ContainsKey(vIdx))
                            {
                                vertexMap[vIdx] = localVertexCount;
                                meshVertices.Add(loader.vertices[vIdx]);
                                meshNormals.Add(face.NormalIndices[i] >= 0 ? loader.normals[face.NormalIndices[i]] : Vector3.zero);
                                meshUVs.Add(face.UVIndices[i] >= 0 ? loader.uvs[face.UVIndices[i]] : Vector2.zero);
                                localVertexCount++;
                            }
                            currentVertices.Add(vertexMap[vIdx]);
                        }

                        // Triangulate face (assuming convex polygon)
                        for (int i = 1; i < face.VertexIndices.Count - 1; i++)
                        {
                            currentTriangles.Add(currentVertices[0]);
                            currentTriangles.Add(currentVertices[i]);
                            currentTriangles.Add(currentVertices[i + 1]);
                        }
                        currentVertices.Clear();
                    }

                    // Create final mesh for this material
                    if (meshVertices.Count > 0)
                    {
                        meshes.Add(CreateMesh(meshVertices, meshNormals, meshUVs, currentTriangles, materialName));
                    }

                    // Create GameObjects for each mesh
                    for (int i = 0; i < meshes.Count; i++)
                    {
                        GameObject subObj = new GameObject($"{name}_mesh_{meshIndex++}");
                        var meshFilter = subObj.AddComponent<MeshFilter>();
                        var meshRenderer = subObj.AddComponent<MeshRenderer>();

                        meshFilter.mesh = meshes[i];
                        meshRenderer.material = loader.Materials != null && loader.Materials.ContainsKey(materialName)
                            ? loader.Materials[materialName]
                            : new Material(Shader.Find("Standard"));

                        subObjects.Add(subObj);
                    }
                }

                return subObjects;
            }

            private Mesh CreateMesh(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles, string materialName)
            {
                Mesh mesh = new Mesh();
                if (vertices.Count > MaxVerticesPerMesh)
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.vertices = vertices.ToArray();
                mesh.normals = normals.ToArray();
                mesh.uv = uvs.ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.name = $"{name}_{materialName}";
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private class MTLLoader
        {
            public Dictionary<string, Material> Load(string mtlPath)
            {
                // Implement MTL parsing or return default materials
                return new Dictionary<string, Material>
                {
                    { "default", new Material(Shader.Find("Standard")) }
                };
            }
        }
    }
}