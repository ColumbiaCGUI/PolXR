using UnityEngine;

namespace LinePicking
{
    public static class RadargramMeshUtils
    {
        /// Approximates UV coordinates from a hit position on a curved mesh
        public static Vector2 ApproximateUVFromHit(Vector3 hitPoint, GameObject meshObj)
        {
            Mesh mesh = meshObj.GetComponent<MeshFilter>().mesh;
            Transform transform = meshObj.transform;

            // Convert hit point to local space
            Vector3 localHitPoint = transform.InverseTransformPoint(hitPoint);

            // Get mesh data
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;

            // Find the closest triangle to the hit point
            float minDistance = float.MaxValue;
            int closestTriangleIndex = -1;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];

                // Calculate the closest point on the triangle
                Vector3 closestPoint = GeometryUtils.ClosestPointOnTriangle(localHitPoint, v1, v2, v3);
                float distance = Vector3.Distance(localHitPoint, closestPoint);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestTriangleIndex = i;
                }
            }

            if (closestTriangleIndex == -1)
            {
                Debug.LogError("Could not find closest triangle to hit point");
                return Vector2.zero;
            }

            // Get the vertices and UVs of the closest triangle
            Vector3 v1Closest = vertices[triangles[closestTriangleIndex]];
            Vector3 v2Closest = vertices[triangles[closestTriangleIndex + 1]];
            Vector3 v3Closest = vertices[triangles[closestTriangleIndex + 2]];

            Vector2 uv1 = uvs[triangles[closestTriangleIndex]];
            Vector2 uv2 = uvs[triangles[closestTriangleIndex + 1]];
            Vector2 uv3 = uvs[triangles[closestTriangleIndex + 2]];

            // Calculate barycentric coordinates
            Vector3 barycentric = GeometryUtils.Barycentric(localHitPoint, v1Closest, v2Closest, v3Closest);

            // Interpolate UV using barycentric coordinates
            return uv1 * barycentric.x + uv2 * barycentric.y + uv3 * barycentric.z;
        }
        
        public static Vector3 GetPointOnRadargramMesh(Vector2 uv, GameObject radargramMesh, string radargramImgName, bool exportDebugImg = false)
        {
            // Get the texture from the mesh renderer's material
            MeshRenderer meshRenderer = radargramMesh.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogError("MeshRenderer component not found on the mesh object");
                return Vector3.zero;
            }

            Texture2D originalTexture = meshRenderer.material.mainTexture as Texture2D;
            if (originalTexture == null)
            {
                Debug.LogError("No texture found on the mesh renderer's material");
                return Vector3.zero;
            }

            FlightlineInfo flightlineInfo = radargramMesh.transform.parent.parent.GetComponentInChildren<FlightlineInfo>();
            if (flightlineInfo == null)
            {
                Debug.LogError("No corresponding flight line object found.");
                return Vector3.zero;
            }

            // Rotate the texture 180 degrees
            Texture2D texture = TextureUtils.ReflectTextureDiagonally(originalTexture);

            // Create a debug texture to visualize the brightest pixels
            Texture2D debugTexture = new Texture2D(texture.width, texture.height);
            if (exportDebugImg)
            {
                Color[] originalPixels = texture.GetPixels();
                debugTexture.SetPixels(originalPixels);
                debugTexture.Apply();
            }

            int h = texture.height;
            int w = texture.width;

            // Convert UV coordinates (Unity's bottom-left origin) to image coordinates (top-left origin)
            int beginX = w - (int)(w * uv.x);
            int beginY = h - (int)(h * uv.y); // Flip Y coordinate for top-left origin

            // Mark the initial picked point on the debug texture
            if (exportDebugImg)
            {
                debugTexture.SetPixel(beginX, beginY, Color.red);
                debugTexture.Apply();
            }

            Vector2 pickedPointUV = new Vector2(1.0f - (float)beginX / w, 1.0f - (float)beginY / h);
            Vector3 point = CoordinateUtils.UvTo3D(pickedPointUV, radargramMesh.GetComponent<MeshFilter>().mesh, radargramMesh.transform);

            // Save the debug texture to a file for inspection
            TextureUtils.SaveDebugTexture(debugTexture, radargramImgName);

            return point;
        }
    }
}