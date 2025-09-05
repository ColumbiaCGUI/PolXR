//using Microsoft.MixedReality.Toolkit.Input;
//using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class RadarEvents : MonoBehaviour
{

    // Pop up menu and the mark object.
    public GameObject Menu;
    public GameObject MarkObj;
    public GameObject MarkObj3D;

    // Measurement tool
    public GameObject MeasureObj;
    public GameObject line;

    // The file root under the "Resources" folder.
    protected bool loaded = false;

    // The transparency value.
    public float alpha = 1.0f;

    // Keep the original scale.
    public float scaleX=1.0f;
    public float scaleY=1.0f;
    public float scaleZ=1.0f;

    // The original transform.
    protected Vector3 position;
    protected Vector3 rotation;

    // The mark shown on the minimap
    
    public GameObject radarMark;
    protected bool newPointAdded = false;
    protected Vector3 newPointPos;
    protected Color markColor;

    GameObject rmZExagText;

    void Start()
    {
        transform.localScale = new Vector3(scaleX,scaleY,scaleZ);
        rmZExagText = GameObject.Find("RadarMenu/Texts/Scaling Sliders/Z Scaling Text");
    }

    // Return the original scale.
    public Vector3 GetScale() { 
        return new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.z); 
    }

    public void SetScaleVar(){
        scaleX = transform.localScale.x;
        scaleY = transform.localScale.y;
        scaleZ = transform.localScale.z;
    }
    public void SetScaleZ(float zScale){
        Vector3 originalScale=GetScale();
        transform.localScale = new Vector3(transform.localScale.x,transform.localScale.y,zScale);
        SetScaleVar();
        rmZExagText.GetComponent<TextMeshPro>().text = string.Format(
                    "Original:   {0} m \n" +
                    "Current:    {1} m \n" +
                    "Strain:     {2}",
                    (originalScale.z).ToString(),
                    (transform.localScale.z).ToString(),
                    (Math.Abs(originalScale.z - transform.localScale.z)).ToString());
    }

    /*
    public void SetScaleXY(float xyScale){
        transform.localScale = new Vector3(xyScale,xyScale, this.transform.localScale.z);
        SetScale();
    }
    */

    // Turn on/off the image itself.
    public void ToggleRadar(bool toggle) { }

    // Resets the radar parent to its original position.
    public void ResetRadar(bool whiten) { }

    // Change the transparancy of the radar images. "onlyLower" used for setting radar only to more transparent level.
    public void SetAlpha(float newAlpha, bool onlyLower = false)
    {
        if ((onlyLower && alpha > newAlpha) || !onlyLower) alpha = newAlpha;
        transform.GetChild(0).gameObject.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f, newAlpha);
        transform.GetChild(1).gameObject.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 1.0f, newAlpha);
    }

    public void UpdateOpacity(float value){
        alpha=value;
        transform.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, alpha);
    }

    // Sychronize the parameters for the main/radar menu.
    public void SychronizeMenu()
    {
        // The menu.
        Vector3 newPosition = Camera.main.transform.position + Camera.main.transform.forward * 0.6f;
        //Menu.transform.GetComponent<MenuEvents>().CloseButton(false);
        //Menu.transform.GetComponent<MenuEvents>().ResetRadarSelected(this.transform, newPosition, alpha);
        //Menu.transform.GetComponent<MenuEvents>().syncScaleSlider();
        //radarMark.SetActive(true);
    }
}