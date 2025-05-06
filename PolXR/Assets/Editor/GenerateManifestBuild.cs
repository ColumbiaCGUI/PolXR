using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
using UnityEngine;

public class GenerateManifestOnBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        GenerateManifest();
    }

    [MenuItem("Tools/Generate StreamingAssets Manifest")]
    public static void GenerateManifest()
    {
        // Define paths
        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, "AppData");
        string manifestPath = Path.Combine(streamingAssetsPath, "manifest.json");

        // Ensure StreamingAssets/AppData exists
        if (!Directory.Exists(streamingAssetsPath))
        {
            Directory.CreateDirectory(streamingAssetsPath);
            Debug.LogWarning($"Created StreamingAssets/AppData directory: {streamingAssetsPath}");
        }

        // Get all files in StreamingAssets/AppData, including subdirectories
        string[] files = Directory.GetFiles(streamingAssetsPath, "*", SearchOption.AllDirectories);

        // Convert to relative paths, excluding .meta, .DS_Store, and manifest files
        var relativePaths = files
            .Select(f => f.Replace(Application.streamingAssetsPath + Path.DirectorySeparatorChar, ""))
            .Where(f => !f.EndsWith(".meta") && !f.EndsWith(".DS_Store") && !f.EndsWith("manifest.json"))
            .Select(f => f.Replace('\\', '/'))
            .ToArray();

        // Create manifest object
        var manifest = new Manifest { files = relativePaths };

        // Write manifest to StreamingAssets/AppData
        string json = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(manifestPath, json);

        // Refresh AssetDatabase to include the manifest in the build
        AssetDatabase.Refresh();

        Debug.Log($"Generated manifest at {manifestPath} with {relativePaths.Length} files.");
    }

    [System.Serializable]
    private class Manifest
    {
        public string[] files;
    }
}