//using Microsoft.MixedReality.Toolkit.Input;
//using Microsoft.MixedReality.Toolkit;
//using Microsoft.MixedReality.Toolkit.UI;
//using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;

public class RadarEvents3D : RadarEvents//, IMixedRealityPointerHandler
{

    // The scientific objects
    public GameObject radargrams;
    public GameObject flightline;

    public GameObject meshForward;
    public GameObject meshBackward;
    public GameObject gridLine;

    // this is used to store all the picked lines for every radargram. 
    //  it will be used for exporting the picked lines along with the radargram
    private Dictionary<int, int[,]> linecoordsxyDict = new Dictionary<int, int[,]>();
    private Dictionary<int, Vector3[]> worldcoordsDict = new Dictionary<int, Vector3[]>();
    public int pickNumber = 0;


    // Start is called before the first frame update
    void Start()
    {

        // Grab relevant objects
        flightline = this.transform.GetChild(1).gameObject;
        radargrams = this.transform.GetChild(2).gameObject;
        //radarMark = this.transform.GetChild(3).gameObject;

        meshForward = radargrams.transform.GetChild(0).gameObject;
        meshBackward = radargrams.transform.GetChild(1).gameObject;
        MarkObj3D = radargrams.transform.GetChild(3).gameObject;

        // Store initial values
        scaleX = radargrams.transform.localScale.x;
        scaleY = radargrams.transform.localScale.y;
        scaleZ = radargrams.transform.localScale.z;
        position = radargrams.transform.localPosition;
        rotation = radargrams.transform.eulerAngles;

        // Add manipulation listeners to the radargrams
        //radargrams.GetComponent<Microsoft.MixedReality.Toolkit.UI.ObjectManipulator>().OnManipulationStarted.AddListener(Select);

        // Set objects to their starting states
        //SetActive(false);
        TogglePolyline(true, false);
        ToggleRadar(false);

    }

    public void TogglePolyline(bool toggle, bool selectRadar)
    {
        // Actually toggle the polyline
        flightline.SetActive(toggle);
        if (!toggle)
        {
            loaded = false;
            return;
        }

        // Render line based on inputs
        LineRenderer lineRenderer = flightline.GetComponent<LineRenderer>();

        // Set color based on selection
        lineRenderer.startColor = lineRenderer.endColor = loaded ?
            selectRadar ?
                new Color(0.1098f, 0.8196f, 0.0588f)       // loaded and selected - Light Green
                : new Color(0.1098f, 0.2705f, 0.0980f)    // loaded, not selected - Dark Green
            : new Color(0.1647f, 0.1647f, 0.1647f);        // not loaded - Dark Gray

    }

    // Turn on/off the 3D surfaces and associated colliders
    public new void ToggleRadar(bool toggle)
    {
        this.transform.GetComponent<BoxCollider>().enabled = !loaded;
        // this.transform.GetComponent<BoundsControl>().enabled = toggle;
        radargrams.SetActive(toggle);
        loaded = toggle;
        MarkObj3D.SetActive(false);
    }

    // Checks if the radargram is currently loaded
    public bool isLoaded()
    {
        return flightline.GetComponent<LineRenderer>().startColor != new Color(1f, 1f, 0f);
    }

    public void Select()
    {
        // Update the menu
        SychronizeMenu();

        // Select the flightline portion
        foreach (RadarEvents3D sibling in this.transform.parent.gameObject.GetComponentsInChildren<RadarEvents3D>())
        {
            sibling.TogglePolyline(true, false);
        }
        TogglePolyline(true, true);
    }

    //private void Select(ManipulationEventData eventData)
    //{
    //    Select();
    //}

    //// Show the menu and mark and update the variables
    //public void OnPointerDown(MixedRealityPointerEventData eventData)
    //{
    //    // Only load the images when selected
    //    ToggleRadar(true);

    //    // Show that the object has been selected
    //    Select();

    //    if (Menu.transform.GetComponent<MenuEvents>().isLinePickingMode)
    //    {
    //        doLinePicking(eventData);
    //    }

    //    Debug.Log("on pointer down is firing");

    //}
    //private void doLinePicking(MixedRealityPointerEventData eventData)
    //{
    //    //create ray, do raycasting
    //    Ray ray = new Ray(eventData.Pointer.Result.Details.Point, -eventData.Pointer.Result.Details.Normal);
    //    RaycastHit[] hits;
    //    hits = Physics.RaycastAll(ray);

    //    if (hits.Length > 0)
    //    {
    //        //sort hits list by distance closest to furthest
    //        Array.Sort(hits, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));

    //        foreach (RaycastHit obj in hits)
    //        {
    //            // find the first radargram mesh that was hit
    //            if (obj.transform.name.StartsWith("Data") || obj.transform.name.StartsWith("_Data"))
    //            { 
    //                //draw mark at point of intersection
    //                //DrawMarkObj(obj);
    //                Vector2 uvCoordinates = obj.textureCoord;

    //                //get the world coordinates of the picked points
    //                Vector3[] worldcoords = GetLinePickingPoints(uvCoordinates, meshForward, obj.transform.name);
                    
    //                //draw line using the picked points
    //                DrawPickedPointsAsLine(worldcoords);
    //                break;
    //            }
    //        }
    //    }
    //}

    // Unused functions
    //public void OnPointerUp(MixedRealityPointerEventData eventData) { }
    //public void OnPointerDragged(MixedRealityPointerEventData eventData) { }
    //public void OnPointerClicked(MixedRealityPointerEventData eventData) { }

    // Gets the scale of the radargram
    public new Vector3 GetScale()
    {
        return new Vector3(radargrams.transform.localScale.x, radargrams.transform.localScale.y, radargrams.transform.localScale.z);
    }

    // Change the transparency of the radar images. "onlyLower" used for setting radar only to more transparent level
    public new void SetAlpha(float newAlpha, bool onlyLower = false)
    {
        if ((onlyLower && alpha > newAlpha) || !onlyLower) alpha = newAlpha;
        for (int i = 0; i < 2; i++)
        {
            if (newAlpha == 1)
                ToOpaqueMode(radargrams.transform.GetChild(i).gameObject.GetComponent<MeshRenderer>().material);
            else
                ToFadeMode(radargrams.transform.GetChild(i).gameObject.GetComponent<MeshRenderer>().material);
            Color color = radargrams.transform.GetChild(i).gameObject.GetComponent<MeshRenderer>().materials[0].color;
            color.a = newAlpha;
            radargrams.transform.GetChild(i).gameObject.GetComponent<MeshRenderer>().materials[0].color = color;
        }
    }

    private static void ToOpaqueMode(Material material)
    {
        material.SetOverrideTag("RenderType", "");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = -1;
    }

    private static void ToFadeMode(Material material)
    {
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    // Just resets the radar transform
    public void ResetTransform()
    {
        radargrams.transform.localPosition = position;
        radargrams.transform.localRotation = Quaternion.Euler(rotation);
        radargrams.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }

    // Reset the radar as if it had not been loaded 
    // THIS IS UNUSED
    public void ResetRadar()
    {
        // Return the radargrams to their original position
        ResetTransform();

        // Turn the radargrams off
        ToggleRadar(false);

        // Ensure the flightline is still on
        TogglePolyline(true, false);

        // Turn off the radar mark
        //radarMark.SetActive(false);
    }

    public void DrawMarkObj(RaycastHit obj)
    {
        // get local position of hit point relative to the radargram
        Vector3 localPosition = radargrams.transform.InverseTransformPoint(obj.point);

        // set the mark object transform to the hit point
        MarkObj3D.SetActive(true);
        MarkObj3D.transform.rotation = radargrams.transform.rotation;
        MarkObj3D.transform.localPosition = localPosition;

    }

    public Vector3[] GetLinePickingPoints(Vector2 uv, GameObject curmesh, string imgname)
    {
        bool isForward = true;

        // if the image name starts with an underscore, it means it is a backward facing radargram
        if (imgname.StartsWith("_"))
        { 
            imgname = imgname.Substring(1);
            isForward = false;
        }
        imgname = imgname + ".png";

        //read in image
        //these ones are horizontal
        // the reason why we don't just directly use the images for the radargram mesh textures is because they are rotated
        string path = Path.Combine("HorizontalRadar", imgname);
        byte[] fileData = BetterStreamingAssets.ReadAllBytes(path);

        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);

        if (texture == null)
        {
            Debug.Log("Couldn't load in radar image");
        }

        int h = (int)texture.height;
        int w = (int)texture.width;

        // Line picking
        int windowSize = 21;
        int halfWin = (int)windowSize / 2;

        //this is correct when origin is top left
        int beginX = (int)(w * uv.y);
        int beginY = (int)(h * uv.x);

        //unity texture has origin at bottom left >:(
        int prevX = beginX;
        int prevY = h - beginY;

        //for debugging reasons we have 2 arrays, but linecoordsxy doesn't really get used
        //it stores the x,y, coordinates of the picked points
        //linecoordsxy will be important for exporting the coordinates
        //we use the uvs array one to draw the line in 3d in Unity

        int[,] linecoordsxy = new int[w, 2];
        Vector2[] uvs = new Vector2[w];

        int maxlocalval = 0;
        int maxlocaly = 0;

        int j = beginX;

        //populate linecoordsxy and uvs with x,y coordinates that are on the line and their corresponding uvs
        //note that coordinate system has shifted with origin at bottom left

        //run loop for all pixels to the right of the picked point
        for (int col = beginX; col < w; col++)
        {
            for (int i = prevY - halfWin; i <= prevY + halfWin; i++)
            {
                byte g = (byte)(255 * texture.GetPixel(col, i).g); //since the image is in black and white, any channel will return the same illuminosity value
                if (maxlocalval < g)
                {
                    maxlocalval = g;
                    maxlocaly = i;
                }
            }
            linecoordsxy[j, 0] = col; // setting x
            linecoordsxy[j, 1] = h - maxlocaly; //setting y, transformed back to origin in top left
            prevY = maxlocaly;
            uvs[j] = new Vector2((float)linecoordsxy[j, 1] / h, (float)linecoordsxy[j, 0] / w);
            maxlocalval = 0;
            j++;
        }

        //run loop again for pixels on the left of the picked point
        maxlocalval = 0;
        maxlocaly = 0;
        prevY = h - beginY;
        j = beginX - 1;
        for (int col = beginX - 1; col >= 0; col--)
        {
            for (int i = prevY - halfWin; i <= prevY + halfWin; i++)
            {
                byte g = (byte)(255 * texture.GetPixel(col, i).g); //since the image is in black and white, any channel will return the same illuminosity value
                if (maxlocalval < g)
                {
                    maxlocalval = g;
                    maxlocaly = i;
                }
            }
            linecoordsxy[j, 0] = col; // setting x
            linecoordsxy[j, 1] = h - maxlocaly; //setting y, transformed back to origin in top left
            prevY = maxlocaly;
            uvs[j] = new Vector2((float)linecoordsxy[j, 1] / h, (float)linecoordsxy[j, 0] / w);
            maxlocalval = 0;
            j--;
        }

        // create a list of world coordinates with the same length as the radargram image width
        Vector3[] worldcoords = new Vector3[w];

        //convert list of uv coordinates to world coords
        for (int i = 0; i < w; i++)
        {
            worldcoords[i] = UvTo3D(uvs[i], curmesh.GetComponent<MeshFilter>().mesh, curmesh.transform);
        }

        // add the 2D pixel coordinates and the world coordinates to their respective dictionaries
        linecoordsxyDict.Add(pickNumber, linecoordsxy);
        worldcoordsDict.Add(pickNumber, worldcoords);

        // increment the pick number
        pickNumber++;

        return worldcoords;
    }

    //this function is used to convert a single uv coordinate to a world coordinate
    public Vector3 UvTo3D(Vector2 uv, Mesh mesh, Transform transform)
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
            float a = Area(u1, u2, u3);
            if (a == 0)
                continue;

            // Calculate barycentric coordinates of u1, u2, and u3
            // If any is negative, point is outside the triangle: skip it
            float a1 = Area(u2, u3, uv) / a;
            if (a1 < 0)
                continue;

            float a2 = Area(u3, u1, uv) / a;
            if (a2 < 0)
                continue;

            float a3 = Area(u1, u2, uv) / a;
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

    //helper function for UvTo3D
    private float Area(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        Vector2 v1 = p1 - p3;
        Vector2 v2 = p2 - p3;
        return (v1.x * v2.y - v1.y * v2.x) / 2;
    }


    public void DrawPickedPointsAsLine(Vector3[] worldcoords)
    {
        List<Vector3> filteredCoords = worldcoords.Where(coord => coord != Vector3.zero).ToList();
        GameObject lineObject = new GameObject("Polyline");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

        // Set LineRenderer properties
        lineRenderer.positionCount = filteredCoords.Count;
        lineRenderer.startWidth = 0.02f; // Adjust the width as needed
        lineRenderer.endWidth = 0.02f;   // Adjust the width as needed

        // Set positions for the line
        lineRenderer.SetPositions(filteredCoords.ToArray());

        // Set the color of the line using the Unlit/Color shader
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        Color lineColor = new Color(0.2f, 0.2f, 1f);
        lineRenderer.SetColors(lineColor, lineColor);

        // Make the drawn line a child of the radargram. World space position, rotation and scale are kept the same as before
        lineRenderer.transform.SetParent(radargrams.transform, true);

        // change useWorldSpace to false so that the line will move alongside the radargram
        lineRenderer.useWorldSpace = false;
    }

    public void saveRadargram()
    {
        saveRadarImg();
        saveRadarCoords();
    }
    private void saveRadarImg()
    {
        // get path of original radargram image to overlay picked points on
        string imgName = radargrams.transform.GetChild(0).name + ".png";

        string path = Path.Combine("HorizontalRadar", imgName);
        byte[] fileData = BetterStreamingAssets.ReadAllBytes(path);

        Texture2D radarimg = new Texture2D(2, 2);
        radarimg.LoadImage(fileData);

        if (radarimg == null)
        {
            Debug.Log("Couldn't load in radar image");
        }

        foreach (int key in linecoordsxyDict.Keys)
        {
            int[,] currentLineCoords = linecoordsxyDict[key];
            int w = radarimg.height;
            // Debug.Log(currentLineCoords.GetLength(0));
            for (int i = 0; i < currentLineCoords.GetLength(0); i++)
            {
                // Debug.Log("x, y: "+ currentLineCoords[i,0] + " " + currentLineCoords[i,1]);
                radarimg.SetPixel(currentLineCoords[i, 0], w - currentLineCoords[i, 1], Color.red);
            }
        }
        radarimg.Apply();

        // Encode the texture as PNG
        byte[] pngBytes = radarimg.EncodeToPNG();

        string outputFolder = Path.Combine(Application.dataPath, "Radargram_picks");

        // Specify the output file path
        string radargramName = radargrams.transform.name.Replace("OBJ_Data_", "");
        string outputPath = Path.Combine(outputFolder, radargramName + "_picks.png");

        // Create the output folder if it doesn't exist
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // Write the PNG bytes to a file
        File.WriteAllBytes(outputPath, pngBytes);

        Debug.Log("Modified radar image exported to: " + outputPath);
    }

    private void saveRadarCoords()
    {
        StringBuilder txtContent = new StringBuilder();

        // Write column headers in the first line
        txtContent.AppendLine("ESPG_X,ESPG_Y,ESPG_Z"); 

        foreach (int key in worldcoordsDict.Keys)
        {
            Vector3[] currentWorldCoords = worldcoordsDict[key];
            Debug.Log("currentWorldCoords: " + currentWorldCoords.Length);
            for (int i = 0; i < currentWorldCoords.Length; i++){
                txtContent.AppendLine(currentWorldCoords[i].x + " " + currentWorldCoords[i].y + " " + currentWorldCoords[i].z);
            }
        }
        string outputFolder = Path.Combine(Application.dataPath, "Radargram_picks");

        // Specify the output file path
        string radargramName = radargrams.transform.name.Replace("OBJ_Data_", "");
        string outputPath = Path.Combine(outputFolder, radargramName + "_picks.txt");

        // Create the output folder if it doesn't exist
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // Write the text content to a .txt file
        File.WriteAllText(outputPath, txtContent.ToString());

        Debug.Log("Text file exported to: " + outputPath);

    }


}
