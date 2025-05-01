using System;
using UniGLTF.MeshUtility;
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

        /// Converts a 3D world coordinate to UV coordinates on a mesh
        public static Vector2 WorldToUV(Vector3 worldPoint, Mesh mesh, Transform transform)
        {
            int[] tris = mesh.triangles;
            Vector2[] uvs = mesh.uv;
            Vector3[] verts = mesh.vertices;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v1 = verts[tris[i]];
                Vector3 v2 = verts[tris[i + 1]];
                Vector3 v3 = verts[tris[i + 2]];

                Debug.Log(v1 + ", " + v2 + ", " + v3);

                // Calculate triangle area in 3D space
                Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
                float area = Vector3.Dot(Vector3.Cross(v2 - v1, v3 - v1), normal) / 2f;
                if (Mathf.Abs(area) < 1e-6f)
                    continue;

                // Calculate barycentric coordinates in 3D space
                float a1 = Vector3.Dot(Vector3.Cross(v2 - worldPoint, v3 - worldPoint), normal) / (2f * area);
                if (a1 < -1e-6f || a1 > 1f + 1e-6f)
                    continue;

                float a2 = Vector3.Dot(Vector3.Cross(v3 - worldPoint, v1 - worldPoint), normal) / (2f * area);
                if (a2 < -1e-6f || a2 > 1f + 1e-6f)
                    continue;

                float a3 = Vector3.Dot(Vector3.Cross(v1 - worldPoint, v2 - worldPoint), normal) / (2f * area);
                if (a3 < -1e-6f || a3 > 1f + 1e-6f)
                    continue;

                // Point is inside this triangle - use same barycentric coordinates to interpolate UV
                Vector2 uv1 = uvs[tris[i]];
                Vector2 uv2 = uvs[tris[i + 1]];
                Vector2 uv3 = uvs[tris[i + 2]];

                return a1 * uv1 + a2 * uv2 + a3 * uv3;
            }

            // Point not found in any triangle
            return Vector2.zero;
        }

        public static Vector3 GetPointOnMesh(Vector2 uv, GameObject radargramMesh, string radargramImgName, bool exportDebugImg = false)
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
            Vector3 point = UvTo3D(pickedPointUV, radargramMesh.GetComponent<MeshFilter>().mesh, radargramMesh.transform);

            // Save the debug texture to a file for inspection
            TextureUtils.SaveDebugTexture(debugTexture, radargramImgName);

            return point;
        }

        public static Vector3[] GetLinePickingPoints(Vector2 startUV, Vector2 endUV, GameObject radargramMesh, string radargramImgName, Vector3 hitNormal, int sampleRate = 1, bool exportDebugImg = false)
        {
            // Get the texture from the mesh renderer's material
            MeshRenderer meshRenderer = radargramMesh.GetComponent<MeshRenderer>();
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

            FlightlineInfo flightlineInfo = radargramMesh.transform.parent.parent.GetComponentInChildren<FlightlineInfo>();
            if (flightlineInfo == null)
            {
                Debug.LogError("No corresponding flight line object found.");
                return Array.Empty<Vector3>();
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

            // Line picking
            int windowSize = 21;
            int halfWin = windowSize / 2;

            // Convert UV coordinates (Unity's bottom-left origin) to image coordinates (top-left origin)
            int beginX = w - (int)(w * startUV.x);
            int beginY = h - (int)(h * startUV.y); // Flip Y coordinate for top-left origin

            int endX = w - (int)(w * endUV.x);
            int endY = h - (int)(h * endUV.y); // Flip Y coordinate for top-left origin

            // Mark the initial picked point on the debug texture
            if (exportDebugImg)
            {
                debugTexture.SetPixel(beginX, beginY, Color.red);
                debugTexture.Apply();
            }

            // Get the initial brightness and calculate the brightness gradient
            byte initialBrightness = TextureUtils.GetPixelBrightness(texture, beginX, beginY);

            bool isFrontFacing = Vector3.Dot(hitNormal, Vector3.forward) > 0f;
            if (flightlineInfo.isBackwards)
                isFrontFacing = !isFrontFacing;

            // Calculate the initial brightness gradient (difference between upper and lower pixels)
            int upperPixelY = Mathf.Clamp(beginY - 1, 0, h - 1);
            int lowerPixelY = Mathf.Clamp(beginY + 1, 0, h - 1);
            byte upperBrightness = TextureUtils.GetPixelBrightness(texture, beginX, upperPixelY);
            byte lowerBrightness = TextureUtils.GetPixelBrightness(texture, beginX, lowerPixelY);
            int initialGradient = lowerBrightness - upperBrightness;

            // Determine the direction to sample based on whether we're hitting the front or back face
            int startX, stepX;
            if (isFrontFacing)
            {
                // For front face, go left in texture space (right in world space)
                startX = beginX;
                // endX = 0; (uncomment if there's no endUV passed in & we want the line to go the remaining distance across the mesh)
                stepX = -sampleRate;
            }
            else
            {
                // For back face, go right in texture space (left in world space)
                startX = beginX;
                // endX = w; (uncomment if there's no endUV passed in & we want the line to go the remaining distance across the mesh)
                stepX = sampleRate;
            }

            // Calculate the number of samples based on the sample rate and direction
            int numSamples;
            if (isFrontFacing)
            {
                numSamples = beginX / sampleRate + 1;
            }
            else
            {
                numSamples = (w - beginX) / sampleRate + 1;
            }

            // Initialize arrays for storing coordinates
            Vector2[] uvs = new Vector2[numSamples];

            int j = 0; // Index for the sampled arrays

            // Process pixels with sampling
            for (int col = startX; (isFrontFacing && col >= endX) || (!isFrontFacing && col < endX); col += stepX)
            {
                // Calculate the target Y position by interpolating between beginY and endY
                float progress = isFrontFacing ?
                    (float)(startX - col) / (startX - endX) :
                    (float)(col - startX) / (endX - startX);
                int targetY = (int)Mathf.Lerp(beginY, endY, progress);

                int closestBrightnessDiff = int.MaxValue;
                int maxLocalY = targetY;

                // Search in the vertical window around the interpolated target Y position
                for (int i = targetY - halfWin; i <= targetY + halfWin; i++)
                {
                    if (i < 0 || i >= h) continue; // Skip out of bounds

                    // Get the brightness of the current pixel
                    byte g = TextureUtils.GetPixelBrightness(texture, col, i);

                    // Calculate the brightness difference
                    int brightnessDiff = Math.Abs(g - initialBrightness);

                    // Calculate the gradient at this pixel
                    int upperY = Mathf.Clamp(i - 1, 0, h - 1);
                    int lowerY = Mathf.Clamp(i + 1, 0, h - 1);
                    byte upperG = TextureUtils.GetPixelBrightness(texture, col, upperY);
                    byte lowerG = TextureUtils.GetPixelBrightness(texture, col, lowerY);
                    int gradient = lowerG - upperG;

                    // Calculate the gradient difference
                    int gradientDiff = Math.Abs(gradient - initialGradient);

                    // Combined score that considers both brightness and gradient similarity
                    // We weight the brightness difference more heavily than the gradient difference
                    int combinedScore = brightnessDiff + (gradientDiff / 2);

                    // Update the best match if this pixel has a better combined score
                    if (combinedScore < closestBrightnessDiff)
                    {
                        closestBrightnessDiff = combinedScore;
                        maxLocalY = i;
                    }
                }

                beginY = maxLocalY;

                // Mark the detected brightest pixel on the debug texture
                if (exportDebugImg)
                {
                    debugTexture.SetPixel(col, maxLocalY, Color.magenta);
                    debugTexture.Apply();
                }

                // Convert back to UV coordinates (Unity's bottom-left origin)
                // Flip the X coordinate back to match Unity's UV space
                uvs[j] = new Vector2(1.0f - (float)col / w, 1.0f - (float)maxLocalY / h);
                j++;
            }

            // Resize arrays to actual number of samples
            Array.Resize(ref uvs, j);

            // Convert UV coordinates to world coordinates
            Vector3[] worldCoords = new Vector3[j];
            for (int i = 0; i < j; i++)
            {
                worldCoords[i] = UvTo3D(uvs[i], radargramMesh.GetComponent<MeshFilter>().mesh, radargramMesh.transform);
            }

            // Save the debug texture to a file for inspection
            TextureUtils.SaveDebugTexture(debugTexture, radargramImgName);

            Debug.Log($"[GetLinePickingPoints] Processed {j} samples with sample rate {sampleRate}, direction: {(isFrontFacing ? "right" : "left")} in world space");
            return worldCoords;
        }
    }
}
