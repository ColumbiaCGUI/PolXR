using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DataLoader))]
public class DataLoaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DataLoader selector = (DataLoader)target;

        // Ensure serialized properties are updated
        serializedObject.Update();

        // DEM Directory Selection
        DrawDEMDirectorySelection(selector);

        // Flightline Directories Selection
        DrawFlightlineDirectoriesSelection(selector);

        // Apply modified properties
        serializedObject.ApplyModifiedProperties();

    }

    private void DrawDEMDirectorySelection(DataLoader selector)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("DEM Directory", EditorStyles.boldLabel);

        // Get immediate subdirectories from Assets/AppData/DEMs
        string basePath = "Assets/AppData/DEMs";
        string[] demDirs = GetImmediateSubdirectories(basePath);

        List<string> demOptions = new List<string>(demDirs);
        demOptions.Insert(0, "Select DEM Directory..."); // Placeholder option

        // Get the index of the currently selected DEM directory
        string currentDemRelative = ConvertToRelative(selector.demDirectoryPath, basePath);
        int demSelectionIndex = Mathf.Max(demOptions.IndexOf(currentDemRelative), 0);

        // Display dropdown for selecting DEM directory
        demSelectionIndex = EditorGUILayout.Popup("DEM Directory", demSelectionIndex, demOptions.ToArray());

        // Update the selector with the full path of the selected DEM directory
        if (demSelectionIndex > 0) // Valid selection
        {
            selector.demDirectoryPath = Path.Combine(basePath, demOptions[demSelectionIndex]);
        }

        GUILayout.Space(10);
        EditorGUILayout.EndVertical();
    }

    private void DrawFlightlineDirectoriesSelection(DataLoader selector)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Flightline Directories", EditorStyles.boldLabel);

        // Ensure the list is initialized
        if (selector.flightlineDirectories == null)
        {
            selector.flightlineDirectories = new List<string>();
        }

        // Get immediate subdirectories from Assets/AppData/Flightlines
        string basePath = "Assets/AppData/Flightlines";
        string[] flightlineDirs = GetImmediateSubdirectories(basePath);

        List<string> flightlineOptions = new List<string>(flightlineDirs);

        // Display each flightline directory with an option to delete
        for (int i = 0; i < selector.flightlineDirectories.Count; i++)
        {
            string currentFlightlineRelative = ConvertToRelative(selector.flightlineDirectories[i], basePath);

            EditorGUILayout.BeginHorizontal();

            // Display dropdown for Flightline directory
            int flightlineSelectionIndex = Mathf.Max(flightlineOptions.IndexOf(currentFlightlineRelative), 0);
            flightlineSelectionIndex = EditorGUILayout.Popup($"Flightline {i + 1}", flightlineSelectionIndex, flightlineOptions.ToArray());

            // Update the selector with the full path of the selected flightline directory
            if (flightlineSelectionIndex >= 0 && flightlineSelectionIndex < flightlineOptions.Count)
            {
                selector.flightlineDirectories[i] = Path.Combine(basePath, flightlineOptions[flightlineSelectionIndex]);
            }

            // Red "Delete" button
            GUIStyle redButtonStyle = new GUIStyle(GUI.skin.button);
            redButtonStyle.normal.textColor = Color.red;
            if (GUILayout.Button("Del", redButtonStyle, GUILayout.Width(50)))
            {
                selector.flightlineDirectories.RemoveAt(i);
                i--; // Adjust index after removal
            }

            EditorGUILayout.EndHorizontal();
        }

        // Button to add another Flightline directory
        if (GUILayout.Button("Add Another Flightline Directory", GUILayout.Width(250)))
        {
            selector.flightlineDirectories.Add(""); // Add an empty entry
        }

        GUILayout.Space(10);
        EditorGUILayout.EndVertical();
    }

    // Helper method to get only immediate subdirectories
    private string[] GetImmediateSubdirectories(string path)
    {
        if (!Directory.Exists(path))
        {
            return new string[0];
        }

        string[] subdirs = Directory.GetDirectories(path);
        for (int i = 0; i < subdirs.Length; i++)
        {
            subdirs[i] = Path.GetFileName(subdirs[i]); // Only the directory name
        }
        return subdirs;
    }

    // Helper method to convert full paths to relative paths
    private string ConvertToRelative(string fullPath, string basePath)
    {
        if (string.IsNullOrEmpty(fullPath) || !fullPath.StartsWith(basePath))
        {
            return string.Empty;
        }

        return fullPath.Substring(basePath.Length + 1); // Get the relative part
    }
}