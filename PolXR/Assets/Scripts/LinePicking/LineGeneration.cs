using System;
using UniGLTF.MeshUtility;
using UnityEngine;

namespace LinePicking
{
    public static class LineGeneration
    {
        public static Vector3[] GetGuidedLinePickingPoints(Vector2 uv1, Vector2 uv2, GameObject radargramMesh, Vector3 hitNormal, int sampleRate = 1, bool exportDebugImg = false)
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
                Debug.LogError("No corresponding flight line object found");
                return Array.Empty<Vector3>();
            }

            if (sampleRate <= 0)
            {
                Debug.LogError("Sample rate must be greater than 0");
                return Array.Empty<Vector3>();
            }

            // Rotate the texture 180 degrees
            Texture2D texture = TextureUtils.ReflectTextureDiagonally(originalTexture);

            // Create a debug texture to visualize the brightest pixels
            Texture2D debugTexture = null;
            if (exportDebugImg)
            {
                debugTexture = new Texture2D(texture.width, texture.height);

                Color[] originalPixels = texture.GetPixels();
                debugTexture.SetPixels(originalPixels);
                debugTexture.Apply();
            }

            int h = texture.height;
            int w = texture.width;

            // Line picking
            int windowSize = 21;
            int halfWin = windowSize / 2;

            Vector2 firstUV = uv1;
            Vector2 secondUV = uv2;
            if (uv1.x > uv2.x)
            {
                firstUV = uv2;
                secondUV = uv1;
            }

            // Convert UV coordinates (Unity's bottom-left origin) to image coordinates (top-left origin)
            int startX = w - (int)(w * firstUV.x);
            int startY = h - (int)(h * firstUV.y); // Flip Y coordinate for top-left origin

            int endX = w - (int)(w * secondUV.x);
            int endY = h - (int)(h * secondUV.y); // Flip Y coordinate for top-left origin

            // Mark the initial picked point on the debug texture
            if (debugTexture)
            {
                debugTexture.SetPixel(startX, startY, Color.red);
                debugTexture.Apply();
            }

            // Get the initial brightness and calculate the brightness gradient
            byte initialBrightness = TextureUtils.GetPixelBrightness(texture, startX, startY);

            bool isFrontFacing = Vector3.Dot(hitNormal, Vector3.forward) > 0f;
            if (flightlineInfo.isBackwards)
                isFrontFacing = !isFrontFacing;

            // Calculate the initial brightness gradient (difference between upper and lower pixels)
            int upperPixelY = Mathf.Clamp(startY - 1, 0, h - 1);
            int lowerPixelY = Mathf.Clamp(startY + 1, 0, h - 1);
            byte upperBrightness = TextureUtils.GetPixelBrightness(texture, startX, upperPixelY);
            byte lowerBrightness = TextureUtils.GetPixelBrightness(texture, startX, lowerPixelY);
            int initialGradient = lowerBrightness - upperBrightness;

            // Determine the direction to sample based on whether we're hitting the front or back face
            int stepX = isFrontFacing ? sampleRate : -sampleRate;

            // Calculate the number of samples based on the sample rate and direction
            int numSamples = Mathf.Abs(endX - startX) / Mathf.Abs(sampleRate) + 1;
            
            // Initialize arrays for storing coordinates
            Vector2[] uvs = new Vector2[numSamples];

            int j = 0; // Index for the sampled arrays

            // Process pixels with sampling
            for (int col = startX; (stepX > 0 && col < endX) || (stepX < 0 && col >= endX); col += stepX)
            {
                // Calculate the target Y position by interpolating between beginY and endY
                float progress = isFrontFacing ?
                    (float)(startX - col) / (startX - endX) :
                    (float)(col - startX) / (endX - startX);
                int targetY = (int)Mathf.Lerp(startY, endY, progress);

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

                startY = maxLocalY;

                // Mark the detected brightest pixel on the debug texture
                if (debugTexture)
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
                worldCoords[i] = CoordinateUtils.UvTo3D(uvs[i], radargramMesh.GetComponent<MeshFilter>().mesh, radargramMesh.transform);
            }

            // Save the debug texture to a file for inspection
            if (debugTexture)
                TextureUtils.SaveDebugTexture(debugTexture, radargramMesh.transform.parent.name);

            return worldCoords;
        }

        /// <summary>
        /// Creates a series of points along a line between two positions on a radargram mesh.
        /// </summary>
        /// <param name="startPoint">Starting point in world space</param>
        /// <param name="endPoint">Ending point in world space</param>
        /// <param name="radargramMesh">The radargram mesh GameObject</param>
        /// <param name="interval">Distance between points in world units (meters)</param>
        /// <returns>Array of Vector3 positions along the line</returns>
        public static Vector3[] GetUnguidedLinePickingPoints(Vector3 startPoint, Vector3 endPoint, GameObject radargramMesh, float interval = 5f)
        {
            Mesh mesh = radargramMesh.GetComponent<MeshFilter>().mesh;
            Transform transform = radargramMesh.transform;

            // Convert world points to UV coordinates
            Vector2 startUV = CoordinateUtils.WorldToUV(startPoint, mesh, transform);
            Vector2 endUV = CoordinateUtils.WorldToUV(endPoint, mesh, transform);

            // We'll use the world distance to determine point count to maintain consistent spacing
            float worldDistance = Vector3.Distance(startPoint, endPoint);
            int numPoints = Mathf.Max(2, Mathf.CeilToInt(worldDistance / interval) + 1);

            Vector3[] points = new Vector3[numPoints];

            // Generate evenly spaced points in UV space and convert back to world space
            for (int i = 0; i < numPoints; i++)
            {
                float t = i / (float)(numPoints - 1);
                Vector2 uvPoint = Vector2.Lerp(startUV, endUV, t);
                points[i] = CoordinateUtils.UvTo3D(uvPoint, mesh, transform);
            }

            return points;
        }
    }
}
