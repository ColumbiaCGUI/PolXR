using UnityEngine;
using TMPro;
using System;

public class MarkObj3D : MonoBehaviour
{
    private Vector3 Original_Scale;
    public LineRenderer circleRenderer;
    public int steps;
    public float radius;

    // The axis for the radar image.
    public LineRenderer verticalAxis;
    public LineRenderer horizontalAxis;
    private float axisX, axisY;

    // The axis labels.
    public Transform coordComponents;
    public Transform labels;
    public GameObject xyAxisLabel;
    public Vector2 intervalXY = new Vector2(5, 50);
    public float gap = 0.06f;
    public bool showAxis;
    private Transform prevParent;

    // Start is called before the first frame update
    void Start()
    {
        //Record the starting scale.
        Original_Scale = this.transform.lossyScale;
        this.gameObject.SetActive(false);

        // Draw circle. https://pastebin.com/ZHG0crvP
        circleRenderer.positionCount = steps;
        for (int currentStep = 0; currentStep < steps; currentStep++)
        {
            float currentRadian = (float)currentStep / (steps - 1) * 2 * Mathf.PI;
            Vector3 currentPosition = new Vector3(radius * Mathf.Cos(currentRadian), radius * Mathf.Sin(currentRadian), 0.0f);
            circleRenderer.SetPosition(currentStep, currentPosition);
        }

        // if (showAxis)
        // {
        //     // Set the width of the line.
        //     verticalAxis.startWidth = 0.01f;
        //     verticalAxis.endWidth = 0.01f;
        //     horizontalAxis.startWidth = 0.01f;
        //     horizontalAxis.endWidth = 0.01f;
        //}
    }

    // Update is called once per frame
    void Update()
    {
        // Adjust the scale according to new parent.
        Vector3 Global_Scale = this.transform.parent.transform.lossyScale;
        this.transform.localScale = new Vector3(
            Original_Scale.x / Global_Scale.x, 
            Original_Scale.y / Global_Scale.y, 
            Original_Scale.z / Global_Scale.z
        );

        // if (showAxis) updateAxis();
    }

    // Update the image axis.
    private void updateAxis(bool forceUpdate = false)
    {
        if (this.transform.parent.tag == "Radar Image")
        {
            // Set up axis.
            axisX = 0.5f + gap / coordComponents.lossyScale.x;
            axisY = 0.5f + gap / coordComponents.lossyScale.y;

            horizontalAxis.SetPosition(0, new Vector3(-0.5f, -axisY, 0));
            horizontalAxis.SetPosition(1, new Vector3(0.5f, -axisY, 0));
            
            verticalAxis.SetPosition(0, new Vector3(-axisX, -0.5f, 0));
            verticalAxis.SetPosition(1, new Vector3(-axisX, 0.5f, 0));

            // Ensure that the new parent is a radar image.
            if (this.transform.parent != prevParent || forceUpdate)
            {
                prevParent = this.transform.parent;
                coordComponents.parent = this.transform.parent;
                coordComponents.transform.localPosition = new Vector3(0, 0, 0);
                coordComponents.transform.localScale = new Vector3(1, 1, 1);
                coordComponents.transform.localEulerAngles = new Vector3(0, 0, 0);
                
                Vector3 radarOriginalScale = coordComponents.parent.GetComponent<RadarEvents>().GetScale();

                // Destroy the prev labels.
                foreach (Transform child in labels) Destroy(child.gameObject);

                // Update axis labels.
                for (int i = 0; i * intervalXY.x < radarOriginalScale.x * 10; i++)
                {
                    GameObject newLabel = Instantiate(xyAxisLabel, labels);
                    // Change "gap * 1.5f" to change where the physical axis is placed
                    newLabel.GetComponent<DynamicLabel>().Initialize(
                        true, 
                        i * intervalXY.x / radarOriginalScale.x / 10 - 0.5f, 
                        gap * 1.5f, 
                        (i * intervalXY.x).ToString() + " km"
                    );
                    
                }
                
                for (int j = 0; (j - 1) * intervalXY.y < radarOriginalScale.y * 100; j++)
                {
                    GameObject newLabel = Instantiate(xyAxisLabel, labels);
                    newLabel.GetComponent<DynamicLabel>().Initialize(
                        false, 
                        j * intervalXY.y / radarOriginalScale.y / 100 - 0.5f, 
                        gap * 1.5f, 
                        (j * intervalXY.y).ToString() + " m"
                    );
                }
            }
        }
    }
}
