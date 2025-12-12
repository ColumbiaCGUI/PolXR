using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class MeshTexturer : MonoBehaviour
{
    [SerializeField] private Texture2D[] bedrock;
    [SerializeField] private Texture2D[] surface;
    private int bedrockTexturePtr;
    private int surfaceTexturePtr;
    // Start is called before the first frame update
    void Start()
    {
        bedrock = Resources.LoadAll<Texture2D>("Textures/Bedrock");
        surface = Resources.LoadAll<Texture2D>("Textures/Surface");
        bedrockTexturePtr = bedrock.Length - 1;
        surfaceTexturePtr = surface.Length - 1;
    }
    public void ApplyTextureToSurface()
    {
        ApplyTexture("surface");
    }
    public void ApplyTextureToBedrock()
    {
        //Debug.Log("BUTTON PRESSED!");
        ApplyTexture("bedrock");
    }
    // Update is called once per frame
    private void ApplyTexture(string name)
    {
        GameObject DEM = GameObject.Find(name);
        if (DEM == null) Debug.LogError("Error: Failed to get DEM");
        var renderers = DEM.GetComponentsInChildren<MeshRenderer>();
        Texture2D[] textureList = bedrock;
        int ptr = 0;
        if (name == "bedrock")
        {
            textureList = bedrock;
            bedrockTexturePtr++;
            bedrockTexturePtr %= bedrock.Length;
            ptr = bedrockTexturePtr;
        }
        else if (name == "surface")
        {
            textureList = surface;
            surfaceTexturePtr++;
            surfaceTexturePtr %= surface.Length;
            ptr = surfaceTexturePtr;
        }
        Debug.Log("Texture in list being applied: " + ptr);
        foreach (var r in renderers)
        {
            r.material.color = Color.white;
            r.material.mainTexture = textureList[ptr];
        }
    }
}