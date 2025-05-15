using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DynamicLabel : MonoBehaviour
{
    public Vector3 Original_Scale = new Vector3(1, 1, 1);

    // true for x direction, false otherwise.
    private bool xyDirection;
    private float alongAxis;
    private float gap;
    public LineRenderer tickMark;

    // Start is called before the first frame update
    void Start()
    {
        tickMark.startWidth = 0.005f;
        Original_Scale /= this.transform.parent.parent.parent.parent.lossyScale.x; // ??
    }

    // Update is called once per frame
    void Update()
    {
        // Adjust the scale according to new parent.
        Vector3 Global_Scale = this.transform.parent.lossyScale;
        float scaleFactor = this.transform.parent.parent.parent.parent.lossyScale.x;
        this.transform.localScale = new Vector3(
                Original_Scale.x / Global_Scale.x, 
                Original_Scale.y / Global_Scale.y, 
                Original_Scale.z / Global_Scale.z
            ) * scaleFactor;
        updatePosition();
    }

    // Update the text and relative position in the start.
    public void Initialize(bool xyInput, float posInput, float gapInput, string content)
    {
        //tickMark.SetPosition(0, new Vector3( , , 0));
        //tickMark.SetPosition(1, new Vector3( , , 0));

        this.GetComponent<TextMeshPro>().text = content;
        xyDirection = xyInput;
        alongAxis = posInput;
        gap = gapInput;

        updatePosition();
        this.gameObject.SetActive(true);


    }

    // Update method for every frame.
    private void updatePosition()
    {
        if (xyDirection)
        {
            this.GetComponent<TextMeshPro>().rectTransform.localPosition = new Vector3(
                alongAxis, -gap / this.transform.parent.lossyScale.y - 0.5f, 0
            );
            tickMark.SetPosition(0, new Vector3(0, 0.3f * gap / this.transform.lossyScale.y, 0));
            tickMark.SetPosition(1, new Vector3(0, 0.7f * gap / this.transform.lossyScale.y, 0));
        }
        else
        {
            this.GetComponent<TextMeshPro>().rectTransform.localPosition = new Vector3(
                -gap / this.transform.parent.lossyScale.x - 0.5f, alongAxis, 0
            );
            tickMark.SetPosition(0, new Vector3(0.3f * gap / this.transform.lossyScale.x, 0, 0));
            tickMark.SetPosition(1, new Vector3(0.7f * gap / this.transform.lossyScale.x, 0, 0));
        }
    }
}
