using System;
using UnityEngine;

namespace LinePicking
{ 
    public static class UVHelpers
    {
        // Approximates UV coordinates from a hit position on a curved mesh
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

        // Converts a single uv coordinate to a world coordinate
        private static Vector3 UvTo3D(Vector2 uv, Mesh mesh, Transform transform)
        {
            int[] tris = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            Vector3[] verts = mesh.vertices;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector2 u1 = uvs[tris[i]];
                Vector2 u2 = uvs[tris[i + 1]];
                Vector2 u3 = uvs[tris[i + 2]];

                // Calculate triangle area - if zero, skip it
                float a = GeometryUtils.GetTriangleArea(u1, u2, u3);
                if (a == 0)
                    continue;

                // Calculate barycentric coordinates of u1, u2, and u3
                // If any is negative, point is outside the triangle: skip it
                float a1 = GeometryUtils.GetTriangleArea(u2, u3, uv) / a;
                if (a1 < 0)
                    continue;

                float a2 = GeometryUtils.GetTriangleArea(u3, u1, uv) / a;
                if (a2 < 0)
                    continue;

                float a3 = GeometryUtils.GetTriangleArea(u1, u2, uv) / a;
                if (a3 < 0)
                    continue;

                // Point inside the triangle - find mesh position by interpolation
                Vector3 p3D = a1 * verts[tris[i]] + a2 * verts[tris[i + 1]] + a3 * verts[tris[i + 2]];

                // Return it in world coordinates
                return transform.TransformPoint(p3D);
            }

            // Point outside any UV triangle
            return Vector3.zero;
        }

        public static Vector3[] GetLinePickingPoints(Vector2 uv, GameObject curmesh, string imgname, int sampleRate = 1, bool exportDebugImg = false)
        {
            // Get the texture from the mesh renderer's material
            MeshRenderer meshRenderer = curmesh.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogError("MeshRenderer component not found on the mesh object");
                return Array.Empty<Vector3>();
            }

            Texture2D originalTexture = meshRenderer.material.mainTexture as Texture2D;
            if (originalTexture == null)
            {
                Debug.LogError("No texture found on the mesh renderer's material");
                return Array.Empty<Vector3>();
            }

            // Rotate the texture 180 degrees
            Texture2D texture = TextureUtils.RotateTexture180(originalTexture);

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

            // Line picking
            int windowSize = 21;
            int halfWin = windowSize / 2;

            // Convert UV coordinates (Unity's bottom-left origin) to image coordinates (top-left origin)
            int beginX = (int)(w * uv.x);
            int beginY = h - (int)(h * uv.y); // Flip Y coordinate for top-left origin

            // Mark the initial picked point on the debug texture
            if (exportDebugImg)
            {
                debugTexture.SetPixel(beginX, beginY, Color.red);
                debugTexture.Apply();
            }

            // Calculate the number of samples based on the sample rate
            int numSamples = (w / sampleRate) + 1;

            // Initialize arrays for storing coordinates
            Vector2[] uvs = new Vector2[numSamples];

            int maxLocalVal;
            int maxLocalY;
            int j = 0; // Index for the sampled arrays

            // Process pixels to the right of the picked point with sampling
            for (int col = beginX; col < w; col += sampleRate)
            {
                maxLocalVal = 0;
                maxLocalY = 0;

                // Search in the vertical window for the brightest pixel
                for (int i = beginY - halfWin; i <= beginY + halfWin; i++)
                {
                    if (i < 0 || i >= h) continue; // Skip out of bounds
                    byte g = (byte)(255 * texture.GetPixel(col, i).g);
                    if (maxLocalVal < g)
                    {
                        maxLocalVal = g;
                        maxLocalY = i;
                    }
                }

                beginY = maxLocalY;

                // Mark the detected brightest pixel on the debug texture
                if (exportDebugImg)
                {
                    debugTexture.SetPixel(col, maxLocalY, Color.green);
                    debugTexture.Apply();
                }

                // Convert back to UV coordinates (Unity's bottom-left origin)
                uvs[j] = new Vector2((float)col / w, 1.0f - (float)maxLocalY / h);
                j++;
            }

            // Process pixels to the left of the picked point with sampling
            beginY = h - (int)(h * uv.y);

            // Start from the sample before the picked point
            int startCol = beginX - sampleRate;
            if (startCol % sampleRate != 0)
            {
                startCol = beginX - (beginX % sampleRate);
            }

            for (int col = startCol; col >= 0; col -= sampleRate)
            {
                maxLocalVal = 0;
                maxLocalY = 0;

                // Search in the vertical window for the brightest pixel
                for (int i = beginY - halfWin; i <= beginY + halfWin; i++)
                {
                    if (i < 0 || i >= h) continue; // Skip out of bounds
                    byte g = (byte)(255 * texture.GetPixel(col, i).g);
                    if (maxLocalVal < g)
                    {
                        maxLocalVal = g;
                        maxLocalY = i;
                    }
                }

                beginY = maxLocalY;

                // Mark the detected brightest pixel on the debug texture
                if (exportDebugImg)
                {
                    debugTexture.SetPixel(col, maxLocalY, Color.green);
                    debugTexture.Apply();
                }

                // Convert back to UV coordinates (Unity's bottom-left origin)
                uvs[j] = new Vector2((float)col / w, 1.0f - (float)maxLocalY / h);
                j++;
            }

            // Resize arrays to actual number of samples
            Array.Resize(ref uvs, j);

            // Convert UV coordinates to world coordinates
            Vector3[] worldCoords = new Vector3[j];
            for (int i = 0; i < j; i++)
            {
                worldCoords[i] = UvTo3D(uvs[i], curmesh.GetComponent<MeshFilter>().mesh, curmesh.transform);
            }

            // Save the debug texture to a file for inspection
            TextureUtils.SaveDebugTexture(debugTexture, imgname);

            Debug.Log($"[GetLinePickingPoints] Processed {j} samples with sample rate {sampleRate}");
            return worldCoords;
        }
    }
}
