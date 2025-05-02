using UnityEngine;

namespace LinePicking
{
    public static class LineRendererUtils
    {
        public static void InitializeLineRenderer(LineRenderer lineRenderer, Color lineColor)
        {
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            
            // Set the color of the line using the Unlit/Color shader
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor; 
        }
    }
}