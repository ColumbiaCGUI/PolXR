using System.Collections.Generic;
using System.Linq;
using System;
using Fusion;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;

public class BakingObjectProvider : NetworkObjectProviderDefault
{
    // For this sample, we are using very high flag values to indicate custom.
    // Other values will fall through the default instantiation handling.
    public const int CUSTOM_PREFAB_FLAG = 100000;

    // The NetworkObjectBaker class can be reused and is Runner independent.
    private static NetworkObjectBaker _baker;
    private static NetworkObjectBaker Baker => _baker ??= new NetworkObjectBaker();
    private Shader radarShader;
    
    private void Start()
    {
        Debug.Log("BakingObjectProvider started!");
    }

    public override NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context, out NetworkObject result)
    {
        result = null;
        Debug.Log($"[DETAILED] AcquirePrefabInstance called with ID: {context.PrefabId.RawValue}");
        
        try {
            radarShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/RadarShader.shader");
            Debug.Log($"[DETAILED] Shader loaded: {radarShader != null}");
            
            if (radarShader == null)
            {
                Debug.LogError("[DETAILED] RadarShader not found! Create it or update the path.");
                return NetworkObjectAcquireResult.Failed;
            }
            
            // Special case for ID 99999 - likely an off-by-one error
            if (context.PrefabId.RawValue == 99999)
            {
                Debug.LogWarning($"Detected prefab ID 99999, redirecting to CUSTOM_PREFAB_FLAG (100000)");
                
                // Create a custom prefab ID
                NetworkPrefabId customPrefabId = new NetworkPrefabId();
                customPrefabId.RawValue = (uint)CUSTOM_PREFAB_FLAG;
                
                // Instead of trying to modify the context directly, handle the logic here
                var go = FlightLineAndRadargram("Assets/AppData/Flightlines/20100324_01", CUSTOM_PREFAB_FLAG);
                var no = go.AddComponent<NetworkObject>();
                go.AddComponent<NetworkedRadargramController>();
                go.AddComponent<NetworkTransform>();
                go.name = $"Our Radargram (from 99999)";

                // Baking is required for the NetworkObject to be valid for spawning.
                Baker.Bake(go);

                // Move the object to the applicable Runner Scene/PhysicsScene/DontDestroyOnLoad
                if (context.DontDestroyOnLoad)
                {
                    runner.MakeDontDestroyOnLoad(go);
                }
                else
                {
                    runner.MoveToRunnerScene(go);
                }

                // We are finished. Return the NetworkObject and report success.
                result = no;
                return NetworkObjectAcquireResult.Success;
            }
            
            // Detect if this is a custom spawn by its high prefabID value we are passing.
            // The Spawn call will need to pass this value instead of a prefab.
            if (context.PrefabId.RawValue >= CUSTOM_PREFAB_FLAG)
            {
                Debug.Log($"[DETAILED] Processing custom prefab with ID: {context.PrefabId.RawValue}");
                
                var go = FlightLineAndRadargram("Assets/AppData/Flightlines/20100324_01", (int)context.PrefabId.RawValue);
                Debug.Log($"[DETAILED] FlightLineAndRadargram returned: {go != null}");
                
                if (go == null) {
                    Debug.LogError("[DETAILED] FlightLineAndRadargram returned null GameObject");
                    return NetworkObjectAcquireResult.Failed;
                }
                
                Debug.Log("[DETAILED] Adding NetworkObject to returned GameObject");
                var no = go.AddComponent<NetworkObject>();
                go.AddComponent<NetworkedRadargramController>();
                go.AddComponent<NetworkTransform>();
                go.name = $"Our Radargram";

                // Baking is required for the NetworkObject to be valid for spawning.
                Baker.Bake(go);

                // Move the object to the applicable Runner Scene/PhysicsScene/DontDestroyOnLoad
                // These implementations exist in the INetworkSceneManager assigned to the runner.
                if (context.DontDestroyOnLoad)
                {
                    runner.MakeDontDestroyOnLoad(go);
                }
                else
                {
                    runner.MoveToRunnerScene(go);
                }

                // We are finished. Return the NetworkObject and report success.
                result = no;
                return NetworkObjectAcquireResult.Success;
            }
        }
        catch (System.Exception ex) {
            Debug.LogError($"[DETAILED] Exception in AcquirePrefabInstance: {ex.Message}\n{ex.StackTrace}");
            return NetworkObjectAcquireResult.Failed;
        }

        // For all other spawns, use the default spawning.
        return base.AcquirePrefabInstance(runner, context, out result);
    }

    private GameObject FlightLineAndRadargram(string flightlineDirectory, /*GameObject parent,*/ int number)
    {
        string[] segmentFolders = Directory.GetDirectories(flightlineDirectory);
        int index = number - 100000;
        
        if (index < 0 || index >= segmentFolders.Length)
        {
            Debug.LogError($"Segment index {index} is out of range. Available segments: 0-{segmentFolders.Length-1}");
            return new GameObject("Invalid Segment");
        }
        
        string segmentFolder = segmentFolders[index];
        string segmentName = Path.GetFileName(segmentFolder);

        Debug.Log($"Trying to spawn segment at index {index}, folder: {segmentName}");

        //GameObject segmentContainer = CreateChildGameObject(segmentName, parent.transform);
        GameObject segmentContainer = new GameObject(segmentName);
        string[] objFiles = Directory.GetFiles(segmentFolder, "*.obj");
        foreach (string objFile in objFiles)
        {
            string fileName = Path.GetFileName(objFile);
            GameObject lineObj = null;

            if (fileName.StartsWith("FlightLine"))
            {
                // Create LineRenderer for Flightline
                lineObj = CreateLineRenderer(objFile, segmentContainer);
            }
            else if (fileName.StartsWith("Data"))
            {
                GameObject radarObj = LoadObj(objFile);
                radarObj.AddComponent<NetworkObject>();
                radarObj.AddComponent<NetworkTransform>();

                //GameObject radarObjLocal = LoadObj(objFile);
                //radarObjLocal.AddComponent<NetworkObject>();
                //NetworkObject radarObj = runner.Spawn(radarObjLocal);


                //NetworkObject radarObj = LoadObj(objFile);
                Debug.Log("FileName starts with data" + fileName);
                Debug.Log($"OBJ file: {objFile}, has mesh child: {radarObj.transform.Find("mesh") != null}");
                if (radarObj != null)
                {
                    ScaleAndRotate(radarObj, 0.0001f, 0.0001f, 0.001f, -90f);

                    // Find and texture the Radar object's mesh
                    Transform meshChild = radarObj.transform.Find("mesh");

                    if (meshChild == null)
                    {
                        Debug.LogWarning($"Creating fallback mesh for {radarObj.name}");
                        meshChild = new GameObject("mesh").transform;
                        meshChild.SetParent(radarObj.transform);
                        meshChild.gameObject.AddComponent<MeshFilter>();
                        meshChild.gameObject.AddComponent<MeshRenderer>();
                    }
                    
                    // REMOVE NETWORK COMPONENTS FROM CHILD - DON'T ADD NESTED NETWORK OBJECTS
                    // meshChild.gameObject.AddComponent<NetworkObject>();
                    // meshChild.gameObject.AddComponent<NetworkTransform>();

                    string texturePath = Path.Combine(segmentFolder, Path.GetFileNameWithoutExtension(objFile) + ".png");
                    if (File.Exists(texturePath))
                    {
                        Texture2D texture = LoadTexture(texturePath);
                        Material material = CreateRadarMaterial(texture);
                        ApplyMaterial(meshChild.gameObject, material);
                    }

                    // Add necessary components to the Radar object
                    //radarObj.AddComponent<ConstraintManager>();
                    //radarObj.AddComponent<BoundsControl>();
                    //radarObj.AddComponent<NearInteractionGrabbable>();
                    //radarObj.AddComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>();
                    //radarObj.AddComponent<RotationAxisConstraint>();

                    // Add necessary components to the parent segment container
                    segmentContainer.AddComponent<BoxCollider>();
                    //segmentContainer.AddComponent<ConstraintManager>();
                    segmentContainer.AddComponent<MyRadarEvents>();
                }
            }
        }
        return segmentContainer;
    }

    private GameObject LoadObj(string objPath)
    //private NetworkObject LoadObj(string objPath)
    {
        GameObject importedObj = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (importedObj == null)
        {
            Debug.LogError($"Failed to load OBJ: {objPath}");
            return null;
        }
        return Instantiate(importedObj);
    }

    private Texture2D LoadTexture(string texturePath)
    {
        byte[] fileData = File.ReadAllBytes(texturePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        return texture;
    }

    private Material CreateRadarMaterial(Texture2D texture)
    {
        Material material = new Material(radarShader);
        material.SetTexture("_MainTex", texture);
        material.SetFloat("_Glossiness", 0f);
        return material;
    }

    private void ApplyMaterial(GameObject obj, Material material)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }
    }

    private GameObject CreateLineRenderer(string objPath, GameObject parentContainer)
    {
        string[] lines = File.ReadAllLines(objPath);
        List<Vector3> vertices = new List<Vector3>();

        int vertexCount = lines.Count(line => line.StartsWith("v "));

        int sampleRate = Mathf.Max(1, vertexCount / 20);

        int index = 0;
        foreach (string line in lines)
        {
            if (line.StartsWith("v "))
            {
                if (index % sampleRate == 0)
                {
                    string[] parts = line.Split(' ');
                    float x = float.Parse(parts[1]) * 0.0001f;
                    float y = float.Parse(parts[3]) * 0.001f;
                    float z = float.Parse(parts[2]) * 0.0001f;

                    vertices.Add(new Vector3(x, y, z));
                }
                index++;
            }
        }

        if (vertices.Count > 1)
        {
            // Rotate the vertices manually by 180 degrees around the global origin
            List<Vector3> rotatedVertices = RotateVertices(vertices, 180);

            // Create a GameObject for the LineRenderer
            GameObject lineObj = CreateChildGameObject("Flightline", parentContainer.transform);

            // Add LineRenderer component
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = rotatedVertices.Count;
            lineRenderer.SetPositions(rotatedVertices.ToArray());

            // Set RadarShader and material properties
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = Color.black;
            lineRenderer.material.SetFloat("_Glossiness", 0f);
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;

            // Add a click handler
            foreach (Transform child in parentContainer.transform)
            {
                if (child.name.StartsWith("Flightline"))
                {
                    lineObj.AddComponent<XRSimpleInteractable>();
                    break;
                }
            }

            // Add a MeshCollider to the LineRenderer
            AttachBoxColliders(lineObj, rotatedVertices.ToArray());

            lineObj.GetComponent<XRSimpleInteractable>().selectEntered.AddListener(TogglePolyline);
            Debug.Log(lineObj.GetComponent<XRSimpleInteractable>().selectEntered.ToString());

            return lineObj;
        }
        else
        {
            Debug.LogWarning($"No vertices found in flightline .obj file: {objPath}");
            return null;
        }
    }
    static void TogglePolyline(SelectEnterEventArgs arg0)
    {
        IXRSelectInteractable selectedObj = arg0.interactableObject;
        IXRSelectInteractor iXRInteractorObj = arg0.interactorObject;

        Debug.Log("selected");
    }

    private void AttachBoxColliders(GameObject lineObj, Vector3[] vertices)
    {
        for (int i = 1; i < vertices.Length; i++)
        {
            // Calculate the line segment
            Vector3 a = vertices[i - 1];
            Vector3 b = vertices[i];

            // Add the collider
            BoxCollider collider = lineObj.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            // Set the collider bounds
            collider.center = (a + b) / 2f;
            collider.size = new Vector3(
                Math.Max(Mathf.Abs(a.x - b.x), 0.2f),
                Math.Max(Mathf.Abs(a.y - b.y), 0.2f),
                Math.Max(Mathf.Abs(a.z - b.z), 0.2f)
            );

            lineObj.GetComponent<XRSimpleInteractable>().colliders.Add(collider);
        }
    }

    private List<Vector3> RotateVertices(List<Vector3> vertices, float angleDegrees)
    {
        List<Vector3> rotatedVertices = new List<Vector3>();

        // Convert the angle to radians
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        // Compute rotation around the global origin
        foreach (Vector3 vertex in vertices)
        {
            float x = vertex.x * Mathf.Cos(angleRadians) - vertex.z * Mathf.Sin(angleRadians);
            float z = vertex.x * Mathf.Sin(angleRadians) + vertex.z * Mathf.Cos(angleRadians);
            rotatedVertices.Add(new Vector3(x, vertex.y, z));
        }

        return rotatedVertices;
    }

    private void ScaleAndRotate(GameObject obj, float scaleX, float scaleY, float scaleZ, float rotationX)
    // private void ScaleAndRotate(NetworkObject obj, float scaleX, float scaleY, float scaleZ, float rotationX)
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
}



public class DataLoaderRunner : NetworkBehaviour
{
    public string demDirectoryPath;
    public List<string> flightlineDirectories;
    private Shader radarShader;
    private GameObject menu;

    public NetworkRunner runner;

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

    void Awake()
    {
        // Load the RadarShader from the specified path
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

        // Process Flightlines
        foreach (string flightlineDirectory in flightlineDirectories)
        {
            ProcessFlightlines(flightlineDirectory, radarContainer);
        }

        DisableAllRadarObjects(radarContainer);
    }

    private void DisableAllRadarObjects(GameObject radarContainer)
    {
        foreach (Transform segment in radarContainer.transform)
        {
            foreach (Transform child in segment)
            {
                if (child.name.StartsWith("Data")) // Radar objects start with "Data"
                {
                    child.gameObject.SetActive(false); // Disable radar objects
                }
            }
        }
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

    private void flightLineAndRadargram(string flightlineDirectory, GameObject parent, int number) {
        string[] segmentFolders = Directory.GetDirectories(flightlineDirectory);
        int index = number - 100000;
        string segmentFolder = segmentFolders[index];
        string segmentName = Path.GetFileName(segmentFolder);

        GameObject segmentContainer = CreateChildGameObject(segmentName, parent.transform);
        string[] objFiles = Directory.GetFiles(segmentFolder, "*.obj");
        foreach (string objFile in objFiles)
        {
            string fileName = Path.GetFileName(objFile);
            GameObject lineObj = null;

            if (fileName.StartsWith("FlightLine"))
            {
                // Create LineRenderer for Flightline
                lineObj = CreateLineRenderer(objFile, segmentContainer);
            }
            else if (fileName.StartsWith("Data"))
            {
                GameObject radarObj = LoadObj(objFile);

                //GameObject radarObjLocal = LoadObj(objFile);
                //radarObjLocal.AddComponent<NetworkObject>();
                //NetworkObject radarObj = runner.Spawn(radarObjLocal);


                //NetworkObject radarObj = LoadObj(objFile);
                Debug.Log("FileName starts with data" + fileName);
                Debug.Log($"OBJ file: {objFile}, has mesh child: {radarObj.transform.Find("mesh") != null}");
                if (radarObj != null)
                {
                    ScaleAndRotate(radarObj, 0.0001f, 0.0001f, 0.001f, -90f);

                    // Find and texture the Radar object's mesh
                    Transform meshChild = radarObj.transform.Find("mesh");
                    if (meshChild == null)
                    {
                        Debug.LogWarning($"Creating fallback mesh for {radarObj.name}");
                        meshChild = new GameObject("mesh").transform;
                        meshChild.SetParent(radarObj.transform);
                        meshChild.gameObject.AddComponent<MeshFilter>();
                        meshChild.gameObject.AddComponent<MeshRenderer>();
                    }

                    // Add necessary components to the parent segment container
                    segmentContainer.AddComponent<BoxCollider>();
                    //segmentContainer.AddComponent<ConstraintManager>();
                    segmentContainer.AddComponent<MyRadarEvents>();
                }
            }
        }
    }


    private void ProcessFlightlines(string flightlineDirectory, GameObject parent)
    {
        string[] segmentFolders = Directory.GetDirectories(flightlineDirectory);
        foreach (string segmentFolder in segmentFolders)
        {
            string segmentName = Path.GetFileName(segmentFolder);

            // Create a container for the segment (e.g., 001, 002)
            GameObject segmentContainer = CreateChildGameObject(segmentName, parent.transform);

            // Process all .obj files in this segment
            string[] objFiles = Directory.GetFiles(segmentFolder, "*.obj");
            foreach (string objFile in objFiles)
            {
                string fileName = Path.GetFileName(objFile);
                GameObject lineObj = null;

                if (fileName.StartsWith("FlightLine"))
                {
                    // Create LineRenderer for Flightline
                    lineObj = CreateLineRenderer(objFile, segmentContainer);
                }
                else if (fileName.StartsWith("Data"))
                {
                    GameObject radarObj = LoadObj(objFile);

                    //GameObject radarObjLocal = LoadObj(objFile);
                    //radarObjLocal.AddComponent<NetworkObject>();
                    //NetworkObject radarObj = runner.Spawn(radarObjLocal);


                    //NetworkObject radarObj = LoadObj(objFile);
                    Debug.Log("FileName starts with data" + fileName);
                    Debug.Log($"OBJ file: {objFile}, has mesh child: {radarObj.transform.Find("mesh") != null}");
                    if (radarObj != null)
                    {
                        ScaleAndRotate(radarObj, 0.0001f, 0.0001f, 0.001f, -90f);

                        // Find and texture the Radar object's mesh
                        Transform meshChild = radarObj.transform.Find("mesh");
                        if (meshChild == null)
                        {
                            Debug.LogWarning($"Creating fallback mesh for {radarObj.name}");
                            meshChild = new GameObject("mesh").transform;
                            meshChild.SetParent(radarObj.transform);
                            meshChild.gameObject.AddComponent<MeshFilter>();
                            meshChild.gameObject.AddComponent<MeshRenderer>();
                        }

                        // Add necessary components to the parent segment container
                        segmentContainer.AddComponent<BoxCollider>();
                        //segmentContainer.AddComponent<ConstraintManager>();
                        segmentContainer.AddComponent<MyRadarEvents>();
                    }
                }
            }
        }
    }

    // CTL Networking
    private GameObject LoadObj(string objPath)
    //private NetworkObject LoadObj(string objPath)
    {
        GameObject importedObj = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (importedObj == null)
        {
            Debug.LogError($"Failed to load OBJ: {objPath}");
            return null;
        }
        return Instantiate(importedObj);
    }

    private Texture2D LoadTexture(string texturePath)
    {
        byte[] fileData = File.ReadAllBytes(texturePath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        return texture;
    }

    private Material CreateRadarMaterial(Texture2D texture)
    {
        Material material = new Material(radarShader);
        material.SetTexture("_MainTex", texture);
        material.SetFloat("_Glossiness", 0f);
        return material;
    }

    private void ApplyMaterial(GameObject obj, Material material)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }
    }

    private GameObject CreateLineRenderer(string objPath, GameObject parentContainer)
    {
        string[] lines = File.ReadAllLines(objPath);
        List<Vector3> vertices = new List<Vector3>();

        int vertexCount = lines.Count(line => line.StartsWith("v "));

        int sampleRate = Mathf.Max(1, vertexCount / 20);

        int index = 0;
        foreach (string line in lines)
        {
            if (line.StartsWith("v "))
            {
                if (index % sampleRate == 0)
                {
                    string[] parts = line.Split(' ');
                    float x = float.Parse(parts[1]) * 0.0001f;
                    float y = float.Parse(parts[3]) * 0.001f;
                    float z = float.Parse(parts[2]) * 0.0001f;

                    vertices.Add(new Vector3(x, y, z));
                }
                index++;
            }
        }

        if (vertices.Count > 1)
        {
            // Rotate the vertices manually by 180 degrees around the global origin
            List<Vector3> rotatedVertices = RotateVertices(vertices, 180);

            // Create a GameObject for the LineRenderer
            GameObject lineObj = CreateChildGameObject("Flightline", parentContainer.transform);

            // Add LineRenderer component
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = rotatedVertices.Count;
            lineRenderer.SetPositions(rotatedVertices.ToArray());

            // Set RadarShader and material properties
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = Color.black;
            lineRenderer.material.SetFloat("_Glossiness", 0f);
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;

            // Add a click handler
            foreach (Transform child in parentContainer.transform)
            {
                if (child.name.StartsWith("Flightline"))
                {
                    lineObj.AddComponent<XRSimpleInteractable>();
                    break;
                }
            }

            // Add a MeshCollider to the LineRenderer
            AttachBoxColliders(lineObj, rotatedVertices.ToArray());

            lineObj.GetComponent<XRSimpleInteractable>().selectEntered.AddListener(TogglePolyline);
            Debug.Log(lineObj.GetComponent<XRSimpleInteractable>().selectEntered.ToString());

            return lineObj;
        }
        else
        {
            Debug.LogWarning($"No vertices found in flightline .obj file: {objPath}");
            return null;
        }
    }
    static void TogglePolyline(SelectEnterEventArgs arg0)
    {
        IXRSelectInteractable selectedObj = arg0.interactableObject;
        IXRSelectInteractor iXRInteractorObj = arg0.interactorObject;

        Debug.Log("selected");
    }

    private void AttachBoxColliders(GameObject lineObj, Vector3[] vertices)
    {
        for (int i = 1; i < vertices.Length; i++)
        {
            // Calculate the line segment
            Vector3 a = vertices[i - 1];
            Vector3 b = vertices[i];

            // Add the collider
            BoxCollider collider = lineObj.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            // Set the collider bounds
            collider.center = (a + b) / 2f;
            collider.size = new Vector3(
                Math.Max(Mathf.Abs(a.x - b.x), 0.2f),
                Math.Max(Mathf.Abs(a.y - b.y), 0.2f),
                Math.Max(Mathf.Abs(a.z - b.z), 0.2f)
            );

            lineObj.GetComponent<XRSimpleInteractable>().colliders.Add(collider);
        }
    }

    private List<Vector3> RotateVertices(List<Vector3> vertices, float angleDegrees)
    {
        List<Vector3> rotatedVertices = new List<Vector3>();

        // Convert the angle to radians
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        // Compute rotation around the global origin
        foreach (Vector3 vertex in vertices)
        {
            float x = vertex.x * Mathf.Cos(angleRadians) - vertex.z * Mathf.Sin(angleRadians);
            float z = vertex.x * Mathf.Sin(angleRadians) + vertex.z * Mathf.Cos(angleRadians);
            rotatedVertices.Add(new Vector3(x, vertex.y, z));
        }

        return rotatedVertices;
    }

    private void ScaleAndRotate(GameObject obj, float scaleX, float scaleY, float scaleZ, float rotationX)
    // private void ScaleAndRotate(NetworkObject obj, float scaleX, float scaleY, float scaleZ, float rotationX)
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
}