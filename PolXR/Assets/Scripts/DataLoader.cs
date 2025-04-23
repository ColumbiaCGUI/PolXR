using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.Hands;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using Fusion;
//using Fusion;
using System.Collections; // Needed for Coroutines


[System.Serializable]
public class Centroid
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class MetaData
{
    public Centroid centroid;
}


[DefaultExecutionOrder(DataLoader.EXECUTION_ORDER)]
public class DataLoader : MonoBehaviour
{
    public const int EXECUTION_ORDER = 99;

    public string demDirectoryPath;
    public List<string> flightlineDirectories;
    private Shader radarShader;
    private GameObject menu;

    private GameObject radarMenu;
    private GameObject mainMenu;

    // Add NetworkRunner field
    public NetworkRunner runner;

    // Directory containing flight line data
    public string flightlineBaseDirectory = "Assets/AppData/Flightlines";

    // Shader for the radargram material
    public Shader radargramShader;

    // List to hold loaded DEM GameObjects
    public List<GameObject> dems = new List<GameObject>();

    // Reference to the DEM shader
    public Shader demShader;

    // Scale factor for DEMs
    public float demScaleFactor = 0.01f;

    // Flag to prevent spawning multiple times
    private bool _spawnedFlightlines = false;

    public Vector3 GetDEMCentroid()
    {
        if (string.IsNullOrEmpty(demDirectoryPath) || !Directory.Exists(demDirectoryPath))
        {
            Debug.LogWarning("DEM directory is not set or doesn't exist.");
            return Vector3.zero;
        }

        string metaFilePath = Path.Combine(demDirectoryPath, "meta.json");

        if (!File.Exists(metaFilePath))
        {
            Debug.LogWarning("meta.json file not found in the DEM directory.");
            return Vector3.zero;
        }

        try
        {
            string jsonContent = File.ReadAllText(metaFilePath);

            MetaData metaData = JsonUtility.FromJson<MetaData>(jsonContent);

            if (metaData?.centroid != null)
            {
                Vector3 centroid = new Vector3(
                    (float)(metaData.centroid.x),
                    (float)(metaData.centroid.y),
                    (float)(metaData.centroid.z)
                );

                Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);

                Vector3 rotatedCentroid = rotation * centroid;

                Vector3 scaledRotatedCentroid = new Vector3(
                    -rotatedCentroid.x * 0.0001f,
                    rotatedCentroid.y * 0.001f,
                    rotatedCentroid.z * 0.0001f
                );

                return scaledRotatedCentroid;
            }
            else
            {
                Debug.LogWarning("Centroid data not found in meta.json.");
                return Vector3.zero;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error reading or parsing meta.json: {ex.Message}");
            return Vector3.zero;
        }
    }

    void Start()
    {
        Debug.Log("[DataLoader] Start called.");

        radarMenu = GameObject.Find("RadarMenu");
        mainMenu = GameObject.Find("MainMenu");

        radarShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/RadarShader.shader");
        if (radarShader == null)
        {
            Debug.LogError("Failed to load RadarShader at Assets/Shaders/RadarShader.shader!");
            return;
        }

        if (string.IsNullOrEmpty(demDirectoryPath))
        {
            Debug.LogError("DEM directory path is not set!");
            return;
        }

        if (flightlineDirectories == null || flightlineDirectories.Count == 0)
        {
            Debug.LogError("No Flightline directories selected!");
            return;
        }

        menu = GameObject.Find("Menu");
        if (menu == null)
        {
            Debug.LogError("Menu GameObject not found!");
            return;
        }

        // Create DEM and Radar containers under Template
        GameObject demContainer = CreateChildGameObject("DEM", transform);
        GameObject radarContainer = CreateChildGameObject("Radar", transform);

        // Process DEMs
        ProcessDEMs(demContainer);

        // Try to find the runner if not assigned in Inspector
        if (runner == null)
        {
            Debug.Log("[DataLoader] Runner not assigned in inspector, attempting to find it...");
            runner = FindAnyObjectByType<NetworkRunner>();
            if (runner == null)
            {
                Debug.LogWarning("[DataLoader] Could not find NetworkRunner in the scene during Start. Will keep trying in coroutine.");
            }
            else
            {
                Debug.Log("[DataLoader] Found NetworkRunner in the scene.");
            }
        }

        // Start the coroutine to wait for the runner and then spawn flightlines
        StartCoroutine(SpawnFlightlinesWhenReady());

        // Set Toggle Functionality
        SetTogglesForMenus();

        // Set Button Functionality
        SetButtonsForMenus();

        DisableMenus();

        Debug.Log("[DataLoader] Start finished, coroutine initiated.");
    }

    private void ProcessDEMs(GameObject parent)
    {
        Debug.Log("DataLoader Process DEMs called!");
        // Check if the selected DEM directory exists
        if (!Directory.Exists(demDirectoryPath))
        {
            Debug.LogError($"DEM directory not found: {demDirectoryPath}");
            return;
        }

        // Get all .obj files in the selected DEM folder
        string[] objFiles = Directory.GetFiles(demDirectoryPath, "*.obj");
        if (objFiles.Length == 0)
        {
            Debug.LogWarning($"No .obj files found in the selected DEM directory: {demDirectoryPath}");
            return;
        }

        foreach (string objFile in objFiles)
        {
            // Extract the file name without extension (e.g., "bedrock")
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(objFile);

            GameObject demObj = LoadObj(objFile);
            if (demObj != null)
            {
                // Name the GameObject after the .obj file (e.g., "bedrock")
                demObj.name = fileNameWithoutExtension;

                if (fileNameWithoutExtension.Equals("bedrock", StringComparison.OrdinalIgnoreCase))
                {
                    Transform childTransform = demObj.transform.GetChild(0);
                    Renderer renderer = childTransform.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = Color.Lerp(Color.black, Color.white, 0.25f);
                    }
                }

                ScaleAndRotate(demObj, 0.0001f, 0.0001f, 0.001f, -90f);

                demObj.transform.SetParent(parent.transform);
            }
        }
    }

    void ToggleSurfaceDEM(bool arg0)
    {
        GameObject surfaceDEM = GameObject.Find("/Managers/DataLoader/DEM/surface");
        surfaceDEM.SetActive(!surfaceDEM.activeSelf);
    }

    void ToggleBaseDEM(bool arg0)
    {
        GameObject baseDEM = GameObject.Find("/Managers/DataLoader/DEM/bedrock");
        baseDEM.SetActive(!baseDEM.activeSelf);
    }

    void ToggleFlightlines(bool arg0)
    {
        // TODO: Update to find networked objects
        Debug.Log("ToggleFlightlines method needs to be updated to work with networked objects");
    }

    void ToggleRadargram(bool arg0)
    {
        // TODO: Update to find networked objects
        Debug.Log("ToggleRadargram method needs to be updated to work with networked objects");
    }

    void ResetRadargram()
    {
        // TODO: Update to work with networked objects
        Debug.Log("ResetRadargram method needs to be updated to work with networked objects");
    }

    void OpenHome()
    {
        mainMenu.SetActive(true);
        radarMenu.SetActive(false);
    }

    void GoToRadargram()
    {
        // TODO: Update to work with networked objects
        Debug.Log("GoToRadargram method needs to be updated to work with networked objects");
    }

    void CloseMainMenu()
    {
        mainMenu.SetActive(false);
    }

    void CloseRadarMenu()
    {
        radarMenu.SetActive(false);
    }

    private void SetTogglesForMenus()
    {
        Toggle radarMenuRadargramToggle = GameObject.Find("RadarMenu/Toggles/Radargram Toggle").GetComponent<Toggle>();
        Toggle radarMenuSurfaceDEMToggle = GameObject.Find("RadarMenu/Toggles/Surface DEM Toggle").GetComponent<Toggle>();

        //BoundingBox Not Implemented
        Toggle mainMenuBoundingBoxToggle = GameObject.Find("MainMenu/Toggles/BoundingBox Toggle").GetComponent<Toggle>();

        Toggle mainMenuFlightlinesToggle = GameObject.Find("MainMenu/Toggles/Flightlines Toggle").GetComponent<Toggle>();
        Toggle mainMenuSurfaceDEMToggle = GameObject.Find("MainMenu/Toggles/Surface DEM Toggle").GetComponent<Toggle>();
        Toggle mainMenuBaseDEMToggle = GameObject.Find("MainMenu/Toggles/Base DEM Toggle").GetComponent<Toggle>();

        radarMenuSurfaceDEMToggle.onValueChanged.AddListener(ToggleSurfaceDEM);
        mainMenuSurfaceDEMToggle.onValueChanged.AddListener(ToggleSurfaceDEM);
        mainMenuBaseDEMToggle.onValueChanged.AddListener(ToggleBaseDEM);

        radarMenuRadargramToggle.onValueChanged.AddListener(ToggleRadargram);
        mainMenuFlightlinesToggle.onValueChanged.AddListener(ToggleFlightlines);
    }

    private void SetButtonsForMenus()
    {
        Button rmClose = GameObject.Find("RadarMenu/Buttons/ButtonClose").GetComponent<Button>();
        rmClose.onClick.AddListener(CloseRadarMenu);
        Button rmReset = GameObject.Find("RadarMenu/Buttons/ButtonReset").GetComponent<Button>(); // NOT IMPLEMENTED
        Button rmWrite = GameObject.Find("RadarMenu/Buttons/ButtonWrite").GetComponent<Button>(); // NOT IMPLEMENTED
        Button rmHome = GameObject.Find("RadarMenu/Buttons/ButtonHome").GetComponent<Button>();
        rmHome.onClick.AddListener(OpenHome);
        Button rmTeleport = GameObject.Find("RadarMenu/Buttons/ButtonTeleport").GetComponent<Button>();
        rmTeleport.onClick.AddListener(GoToRadargram);
        Button rmResetRadar = GameObject.Find("RadarMenu/Buttons/ButtonResetRadar").GetComponent<Button>();
        rmResetRadar.onClick.AddListener(ResetRadargram);
        Button rmMeasure = GameObject.Find("RadarMenu/Buttons/ButtonMeasure").GetComponent<Button>(); // NOT IMPLEMENTED

        Button mmWrite = GameObject.Find("MainMenu/Buttons/ButtonWrite").GetComponent<Button>(); // NOT IMPLEMENTED
        Button mmReset = GameObject.Find("MainMenu/Buttons/ButtonReset").GetComponent<Button>(); // NOT IMPLEMENTED
        Button mmClose = GameObject.Find("MainMenu/Buttons/ButtonClose").GetComponent<Button>();
        mmClose.onClick.AddListener(CloseMainMenu);
        Button mmMiniMap = GameObject.Find("MainMenu/Buttons/ButtonMiniMap").GetComponent<Button>(); // NOT IMPLEMENTED
        Button mmLoadScene = GameObject.Find("MainMenu/Buttons/ButtonLoadScene").GetComponent<Button>(); // NOT IMPLEMENTED
        Button mmHomeScreen = GameObject.Find("MainMenu/Buttons/ButtonHomeScreen").GetComponent<Button>(); // NOT IMPLEMENTED
    }

    void DisableMenus()
    {
        radarMenu.SetActive(false);
        mainMenu.SetActive(false);
    }

    // Keep utility methods used by DEM processing
    private GameObject LoadObj(string objPath)
    {
        GameObject importedObj = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (importedObj == null)
        {
            Debug.LogError($"Failed to load OBJ: {objPath}");
            return null;
        }
        return Instantiate(importedObj);
    }

    private void ScaleAndRotate(GameObject obj, float scaleX, float scaleY, float scaleZ, float rotationX)
    {
        obj.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
        obj.transform.eulerAngles = new Vector3(rotationX, 0f, 0f);
    }

    private GameObject CreateChildGameObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        return obj;
    }

    // Coroutine to wait for the NetworkRunner and spawn flightlines
    private IEnumerator SpawnFlightlinesWhenReady()
    {
        Debug.Log("[DataLoader Coroutine] Waiting for NetworkRunner...");

        // 1. Wait until the runner exists and is assigned
        while (runner == null)
        {
            Debug.Log("[DataLoader Coroutine] NetworkRunner instance is null, trying to find again...");
            runner = FindAnyObjectByType<NetworkRunner>(); // Keep trying to find it
            if (runner == null)
            {
                yield return new WaitForSeconds(0.5f); // Wait before checking again
            }
            else
            {
                Debug.Log("[DataLoader Coroutine] Found NetworkRunner instance.");
            }
        }

        // 2. Wait until the runner is Running (adjust state if needed, e.g., Started, ClientJoined)
        //    Make sure the runner has had a chance to initialize its internal state.
        yield return new WaitForSeconds(0.1f); // Small initial delay
        while (runner.State != NetworkRunner.States.Running)
        {
            Debug.Log($"[DataLoader Coroutine] Waiting for NetworkRunner to be Running. Current state: {runner.State}");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"[DataLoader Coroutine] NetworkRunner is Running!");

        // 3. Ensure we only spawn once and only if we are the first player
        if (_spawnedFlightlines)
        {
            Debug.Log("[DataLoader Coroutine] Flightlines already marked as spawned, exiting.");
            yield break; // Already spawned, exit coroutine
        }

        // --- Spawning Logic ---
        Debug.Log("[DataLoader Coroutine] Proceeding with Flightline spawning...");

        // Shared Mode: Spawn scene objects only if we are the first player to join.
        if (runner.ActivePlayers.Count() == 1)
        {
            Debug.Log("[DataLoader Coroutine] Active player count is 1. Spawning flightlines...");
            for (int i = 0; i < flightlineDirectories.Count; i++)
            {
                string flightlineDirectory = flightlineDirectories[i];
                if (Directory.Exists(flightlineDirectory))
                {
                    // Generate the custom ID based on the BakingObjectProvider's flag + index
                    uint customPrefabId = (uint)(BakingObjectProvider.CUSTOM_PREFAB_FLAG + i);
                    NetworkPrefabId prefabId = new NetworkPrefabId { RawValue = customPrefabId }; // Correctly create NetworkPrefabId

                    Debug.Log($"[DataLoader Coroutine] Requesting spawn for flightline {i} from directory '{flightlineDirectory}' with PrefabId: {prefabId.RawValue}");

                    try
                    {
                        // Spawn the object associated with the custom prefab ID
                        // The BakingObjectProvider should handle the creation based on this ID
                        runner.Spawn(
                             prefabId,
                             position: Vector3.zero, // Or adjust position as needed
                             rotation: Quaternion.identity, // Or adjust rotation as needed
                             inputAuthority: PlayerRef.None, // Scene object, no specific player authority
                             (runner, obj) =>
                             {
                                 // This optional callback is executed *after* the object is spawned
                                 // and *before* Spawned() is called on the object's behaviours.
                                 // We can pass data here if needed, e.g., the directory path.

                                 // Temporarily comment out until FlightLineAndRadargram compiles correctly
                                 /*
                                 var flightLineComponent = obj.GetComponent<FlightLineAndRadargram>();
                                 if (flightLineComponent != null)
                                 {
                                     Debug.Log($"[DataLoader Coroutine OnBeforeSpawned] Setting directory '{flightlineDirectory}' for spawned object {obj.Id}");
                                     // Pass the directory path *before* Spawned is called on FlightLineAndRadargram
                                     flightLineComponent.InitializeFromDataLoader(flightlineDirectory, radargramShader);
                                 }
                                 else
                                 {
                                     Debug.LogWarning($"[DataLoader Coroutine OnBeforeSpawned] Spawned object {obj.Id} does not have a FlightLineAndRadargram component.");
                                 }
                                 */
                                 Debug.LogWarning($"[DataLoader Coroutine OnBeforeSpawned] FlightLineAndRadargram component access temporarily disabled for object {obj.Id}.");

                             }
                         );
                        Debug.Log($"[DataLoader Coroutine] Spawn call successful for PrefabId {prefabId.RawValue}.");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[DataLoader Coroutine] Exception during runner.Spawn for PrefabId {prefabId.RawValue}: {e.Message}\n{e.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[DataLoader Coroutine] Flightline directory not found, skipping spawn: {flightlineDirectory}");
                }
            }
            _spawnedFlightlines = true; // Mark as spawned after attempting all
            Debug.Log("[DataLoader Coroutine] Finished spawning flightlines (Player Count == 1).");
        }
        else
        {
            Debug.Log($"[DataLoader Coroutine] Active player count is {runner.ActivePlayers.Count()}. Skipping flightline spawning.");
            // Mark as done even if we didn't spawn, to prevent trying again.
            _spawnedFlightlines = true;
        }
        // --- End Spawning Logic ---
    }
}