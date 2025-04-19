using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

namespace LinePicking
{
    public class PickLine : MonoBehaviour
{
    [SerializeField] private InputActionReference linePickingTrigger;

    [SerializeField] private XRRayInteractor rightControllerRayInteractor;

    [SerializeField] private GameObject markObjPrefab;

    private ToggleLinePickingMode _toggleLinePickingMode;

    private bool _isPickingLine;

    [Header("Customization")]

    // The amount of pixels between each point on the line. Higher values will result in less accurate lines, but should generate them more quickly.
    public int pixelsBetweenPoints = 10;

    public Color lineColor = new(0.2f, 0.2f, 1f);

    private void Start()
    {
        BetterStreamingAssets.Initialize();

        _toggleLinePickingMode = GetComponent<ToggleLinePickingMode>();
    }

    private void OnEnable()
    {
        linePickingTrigger.action.started += OnLinePickStart;
        linePickingTrigger.action.canceled += OnLinePickEnd;
    }

    private void OnDisable()
    {
        linePickingTrigger.action.started -= OnLinePickStart;
        linePickingTrigger.action.canceled -= OnLinePickEnd;
    }

    // On trigger press, mark start of line picking
    private void OnLinePickStart(InputAction.CallbackContext context)
    {
        if (!_toggleLinePickingMode.isLinePickingEnabled) return;

        if (rightControllerRayInteractor.TryGetCurrent3DRaycastHit(out var raycastHit))
        {
            Transform hitRadargram = raycastHit.transform;
            bool isRadargramMesh = hitRadargram.name.Contains("Data");
            if (!isRadargramMesh) return;

            _isPickingLine = true;

            // Get the mesh object that was hit
            GameObject meshObj = hitRadargram.GetChild(0).gameObject;

            // Approximate UV coordinates from hit position
            Vector2 uvCoordinates = UVHelpers.ApproximateUVFromHit(raycastHit.point, meshObj);

            Vector3[] worldCoords = UVHelpers.GetLinePickingPoints(uvCoordinates, meshObj, hitRadargram.name, pixelsBetweenPoints);
            GameObject markObj = Instantiate(markObjPrefab, raycastHit.point, hitRadargram.rotation);
            markObj.transform.parent = hitRadargram;

            DrawPickedPointsAsLine(worldCoords, hitRadargram);
        }
    }

    public void DrawPickedPointsAsLine(Vector3[] worldCoords, Transform radargramTransform)
    {
        List<Vector3> filteredCoords = worldCoords.Where(coord => coord != Vector3.zero).ToList();
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
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

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
    }

    // On trigger release, mark end of line picking
    private void OnLinePickEnd(InputAction.CallbackContext context)
    {
        if (!_isPickingLine) return;

        Debug.Log("Line pick end");
    }
}
}
