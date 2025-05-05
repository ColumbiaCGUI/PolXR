using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LinePicking
{
    public static class LinePickUtils
    {
        public static GameObject DrawPickedPointsAsLine(Vector3[] worldCoords, Transform radargramTransform, Color lineColor)
        {
            List<Vector3> filteredCoords = worldCoords.Where(coord => coord != Vector3.zero).ToList();
            GameObject lineObject = new GameObject("Polyline");
            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

            lineRenderer.positionCount = filteredCoords.Count;
            lineRenderer.SetPositions(filteredCoords.ToArray());
    
            // Set the color of the line using the Unlit/Color shader
            LineRendererUtils.InitializeLineRenderer(lineRenderer, lineColor);

            // Make the drawn line a child of the radargram
            lineObject.transform.SetParent(radargramTransform, false);

            // Convert world positions to local positions
            Vector3[] localPositions = new Vector3[filteredCoords.Count];
            for (int i = 0; i < filteredCoords.Count; i++)
            {
                localPositions[i] = radargramTransform.InverseTransformPoint(filteredCoords[i]);
            }

            // Set the local positions
            lineRenderer.SetPositions(localPositions);

            // Now we can safely set useWorldSpace to false
            lineRenderer.useWorldSpace = false;

            return lineObject;
        }

    }
}