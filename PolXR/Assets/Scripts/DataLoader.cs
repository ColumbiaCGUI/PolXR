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


public class DataLoader : MonoBehaviour
{
    public string demDirectoryPath;
    public List<string> flightlineDirectories;
    private Shader radarShader;
    private GameObject menu;

    private GameObject radarMenu;
    private GameObject mainMenu;

    // public NetworkRunner runner;

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

        // Process Flightlines
        foreach (string flightlineDirectory in flightlineDirectories)
        {
            ProcessFlightlines(flightlineDirectory, radarContainer);
            // Debug.LogError(flightlineDirectory);
        }

        // Set Toggle Functionality
        SetTogglesForMenus();

        // Set Button Functionality
        SetButtonsForMenus();

        DisableAllRadarObjects(radarContainer);


        DisableMenus();
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
                    int RadarGramLayer = LayerMask.NameToLayer("Radargram");
                    lineObj.layer = RadarGramLayer;
                }
                else if (fileName.StartsWith("Data"))
                {
                    GameObject radarObj = LoadObj(objFile);

                    if (radarObj != null)
                    {
                        ScaleAndRotate(radarObj, 0.0001f, 0.0001f, 0.001f, -90f);

                        // Find and texture the Radar object's mesh
                        Transform meshChild = radarObj.transform.Find("mesh");
                        if (meshChild != null)
                        {
                            string texturePath = Path.Combine(segmentFolder, Path.GetFileNameWithoutExtension(objFile) + ".png");
                            if (File.Exists(texturePath))
                            {
                                Texture2D texture = LoadTexture(texturePath);
                                Material material = CreateRadarMaterial(texture);
                                ApplyMaterial(meshChild.gameObject, material);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Radar object '{radarObj.name}' does not have a child named 'mesh'.");
                        }

                        // Parent the Radar object to the segment container
                        radarObj.transform.SetParent(segmentContainer.transform);

                        // Add necessary components to the Radar object

                        // Attach the Box Collider
                        GameObject radarMesh = meshChild.gameObject;

                        //BoxCollider bc = radarMesh.AddComponent<BoxCollider>();
                        //Vector3 meshExtents = radarMesh.GetComponentInChildren<MeshRenderer>().bounds.extents;
                        //bc.size = new Vector3(meshExtents.x, meshExtents.y, meshExtents.z);
                        //Debug.Log(bc.size);

                        MeshCollider bc = radarMesh.AddComponent<MeshCollider>();
                        bc.convex = true;

                        // Attach the Grab Interactable
                        XRGrabInteractable IradarObj = radarMesh.AddComponent<XRGrabInteractable>();
                        IradarObj.interactionLayers = InteractionLayerMask.NameToLayer("Radargram");
                        // Add Rotation Constraints for Y Axis Only
                        IradarObj.movementType = XRBaseInteractable.MovementType.Instantaneous;
                        //IradarObj.trackPosition = true;
                        //IradarObj.trackRotation = false;
                        // IradarObj.trackScale = false;
                        IradarObj.throwOnDetach = false;
                        //IradarObj.matchAttachRotation = false;
                        IradarObj.useDynamicAttach = true;

                        radarMesh.GetComponent<Rigidbody>().useGravity = false;
                        radarMesh.GetComponent<Rigidbody>().isKinematic = false;
                        // radarObj.GetComponent<Rigidbody>().freezeRotation = false;

                        // LockObj.canProcess = true;

                        IradarObj.firstSelectEntered.AddListener(ConvertRadargramToWorld);
                        IradarObj.lastSelectExited.AddListener(ResetRadargram);

                        //XRGeneralGrabTransformer IradarGrabTransformer = radarMesh.AddComponent<XRGeneralGrabTransformer>();
                        //GrabTransformerRotationAxisLock LockObj = radarMesh.AddComponent<GrabTransformerRotationAxisLock>(); //Sample Script Changed

                        // Add NetworkedObjectManipulator


                        // Set layer
                        int RadarGramLayer = LayerMask.NameToLayer("Radargram");
                        radarMesh.layer = RadarGramLayer;
                    }
                }
            }
        }
    }

    void ConvertRadargramToWorld(SelectEnterEventArgs args)
    {
        // Actually toggle the polyline
        IXRSelectInteractable component = args.interactableObject;
        IXRSelectInteractor interactor = args.interactorObject;

        Transform meshTransform = component.transform;
        Debug.Log(meshTransform.name);

    }

    // Need to reset Radargram interactable back to position 0,0,0, rotation 0,0,0, scale 1,1,1
    void ResetRadargram(SelectExitEventArgs args)
    {
        // Actually toggle the polyline
        IXRSelectInteractable component = args.interactableObject;
        IXRSelectInteractor interactor = args.interactorObject;

        Transform radargramMesh = component.transform;
        Debug.Log(radargramMesh.name);
        Debug.Log(radargramMesh.transform);

        radargramMesh.localPosition = new Vector3(radargramMesh.localPosition.x / 10000,
            radargramMesh.localPosition.y / 10000, radargramMesh.position.z / 1000);
        radargramMesh.localEulerAngles = new Vector3(0, 0, 0); // TODO: Does not account for rotation properly
        // radargramMesh.localRotation = Quaternion.identity;
        // radargramMesh.localPosition = new Vector3(0, 0, 0);
        radargramMesh.localScale = Vector3.one; // TODO : Needs to be changed if radargram is scaled
    }

    void OpenRadarMenu(SelectExitEventArgs args)
    {
        // Actually toggle the polyline
        IXRSelectInteractable component = args.interactableObject;
        IXRSelectInteractor interactor = args.interactorObject;

        Transform radargramMesh = component.transform;
        Debug.Log(radargramMesh.name);
        Debug.Log(radargramMesh.transform);

        mainMenu.SetActive(false);
        radarMenu.SetActive(true);
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

            // Add a MeshCollider to the LineRenderer
            AttachBoxColliders(lineObj, rotatedVertices.ToArray());

            // Add a click handler
            foreach (Transform child in parentContainer.transform)
            {
                if (child.name.StartsWith("Flightline"))
                {

                    lineObj.AddComponent<NetworkedObjectManipulator>();
                    lineObj.AddComponent<NetworkObject>();
                    NetworkObject no = lineObj.GetComponent<NetworkObject>();
                    // Allow state authority override on the network object
                    if (no != null)
                    {
                        no.Flags |= NetworkObjectFlags.AllowStateAuthorityOverride;
                    }
                    else
                    {
                        Debug.LogError("Failed to add NetworkObject to lineObj");
                    }
                    lineObj.AddComponent<NetworkTransform>();

                    break;
                }
            }

            NetworkedObjectManipulator m_Interactable = lineObj.GetComponent<NetworkedObjectManipulator>();
            m_Interactable.firstSelectEntered.AddListener(TogglePolyline);

            return lineObj;
        }
        else
        {
            Debug.LogWarning($"No vertices found in flightline .obj file: {objPath}");
            return null;
        }
    }

    void TogglePolyline(SelectEnterEventArgs args)
    {
        // Actually toggle the polyline
        IXRSelectInteractable component = args.interactableObject;
        IXRSelectInteractor interactor = args.interactorObject;

        Transform flightlineContainer = component.transform.parent.transform;
        Debug.Log(flightlineContainer.name);

        foreach (Transform child in flightlineContainer)
        {
            if (child.name.StartsWith("Data"))
            {
                Debug.Log(child.name);
                child.gameObject.SetActive(!child.gameObject.activeSelf);

                Transform meshChild = child.transform.Find("mesh");

                meshChild.localRotation = Quaternion.identity;
                meshChild.localPosition = new Vector3(0, 0, 0);
                meshChild.localScale = Vector3.one;
            }
            else if (child.name.StartsWith("Flightline"))
            {
                if (child.gameObject.GetComponent<LineRenderer>().material.color == Color.black)
                {
                    child.gameObject.GetComponent<LineRenderer>().material.color = Color.green;
                    radarMenu.SetActive(true);
                }
                else
                {
                    child.gameObject.GetComponent<LineRenderer>().material.color = Color.black;
                    radarMenu.SetActive(false);
                }
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
        Transform radar = GameObject.Find("Managers/DataLoader/Radar").transform;

        foreach (Transform child in radar)
        {
            foreach (Transform child2 in child)
            {
                if (child2.name.StartsWith("Flightline"))
                {
                    child2.gameObject.SetActive(arg0);
                }
            }
        }
    }

    void ToggleRadargram(bool arg0)
    {
        // TODO
    }

    void ResetRadargram()
    {
        // TODO
    }

    void OpenHome()
    {
        mainMenu.SetActive(true);
        radarMenu.SetActive(false);
    }

    void GoToRadargram()
    {
        // TODO
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

    private void AttachBoxColliders(GameObject lineObj, Vector3[] vertices)
    {
        for (int i = 1; i < vertices.Length; i++)
        {
            // Calculate the line segment
            Vector3 a = vertices[i - 1];
            Vector3 b = vertices[i];

            // Add the collider
            BoxCollider collider = lineObj.AddComponent<BoxCollider>();
            //collider.isTrigger = false;

            // Set the collider bounds
            collider.center = (a + b) / 2f;
            collider.size = new Vector3(
                Math.Max(Mathf.Abs(a.x - b.x), 0.2f),
                Math.Max(Mathf.Abs(a.y - b.y), 0.2f),
                Math.Max(Mathf.Abs(a.z - b.z), 0.2f)
            );

            // lineObj.GetComponent<XRSimpleInteractable>().colliders.Add(collider);
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