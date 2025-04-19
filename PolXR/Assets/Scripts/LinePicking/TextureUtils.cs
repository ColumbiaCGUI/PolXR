using UnityEngine;

namespace LinePicking
{
    public static class TextureUtils
    {
        public static Texture2D RotateTexture180(Texture2D originalTexture)
        {
            int width = originalTexture.width;
            int height = originalTexture.height;

            // Create a new texture with the same dimensions
            Texture2D rotatedTexture = new Texture2D(width, height);

            // Get the original pixels
            Color[] originalPixels = originalTexture.GetPixels();
            Color[] rotatedPixels = new Color[originalPixels.Length];

            // Rotate the pixels 180 degrees
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int originalIndex = y * width + x;
                    int rotatedIndex = (height - 1 - y) * width + (width - 1 - x);
                    rotatedPixels[rotatedIndex] = originalPixels[originalIndex];
                }
            }

            // Apply the rotated pixels to the new texture
            rotatedTexture.SetPixels(rotatedPixels);
            rotatedTexture.Apply();

            return rotatedTexture;
        }
        
        public static void SaveDebugTexture(Texture2D texture, string baseName)
        {
            try
            {
                // Convert texture to PNG
                byte[] bytes = texture.EncodeToPNG();

                // Create a unique filename with timestamp
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string filename = $"DebugTexture_{baseName}_{timestamp}.png";

                // Save to the persistent data path
                string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
                System.IO.File.WriteAllBytes(path, bytes);

                Debug.Log($"Debug texture saved to: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save debug texture: {e.Message}");
            }
        }
    }
}