using System;
using System.IO;
using UnityEngine;

namespace LinePicking
{
    public static class TextureUtils
    {
        /// <summary>
        /// Reflects a texture along the line y=x.
        /// 
        /// In other words, this flips a texture along the x and y axes.
        /// </summary>
        /// <param name="originalTexture"></param>
        /// <returns></returns>
        public static Texture2D ReflectTextureDiagonally(Texture2D originalTexture)
        {
            int width = originalTexture.width;
            int height = originalTexture.height;

            // Create a new texture with the same dimensions
            Texture2D flippedTexture = new Texture2D(width, height);

            // Get the original pixels
            Color[] originalPixels = originalTexture.GetPixels();
            Color[] flippedPixels = new Color[originalPixels.Length];

            // Flip the pixels on both x and y axes
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int originalIndex = y * width + x;
                    int flippedIndex = (height - 1 - y) * width + (width - 1 - x);
                    flippedPixels[flippedIndex] = originalPixels[originalIndex];
                }
            }

            // Apply the flipped pixels to the new texture
            flippedTexture.SetPixels(flippedPixels);
            flippedTexture.Apply();

            return flippedTexture;
        }

        public static void SaveDebugTexture(Texture2D texture, string baseName)
        {
            try
            {
                // Convert texture to PNG
                byte[] bytes = texture.EncodeToPNG();

                // Create a unique filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"DebugTexture_{baseName}_{timestamp}.png";

                // Save to the persistent data path
                string path = Path.Combine(Application.persistentDataPath, filename);
                File.WriteAllBytes(path, bytes);

                Debug.Log($"Debug texture saved to: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save debug texture: {e.Message}");
            }
        }

        public static byte GetPixelBrightness(Texture2D texture, int x, int y)
        {
            return (byte)(255 * texture.GetPixel(x, y).g);
        }
    }
}