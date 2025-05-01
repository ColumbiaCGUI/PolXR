using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UniGLTF.MeshUtility;
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

        private bool _inLinePickingMode;
        
        private Coroutine _continuousLinePickingCoroutine;

        private Dictionary<Vector3, GameObject> _linePointsToGameObjs = new();
        private Dictionary<Vector3, LinePickingPointInfo> _currentLinePickingPoints = new();

        private Transform _currentRadargram;

        [FormerlySerializedAs("pixelsBetweenPoints")] [Header("Customization")]
        
        //. The amount of pixels between each manually picked point.
        public int linePickingHorizontalInterval = 30;
        
        /// Corresponds to automatically generated line points (which fall in between manually picked points.
        /// The amount of pixels between each point on the line. Higher values will result in less accurate lines, but should generate them more quickly.
        public int pixelsBetweenLinePoints = 10;

        public Color lineColor = new(0.2f, 0.2f, 1f);

        /// during line picking: ms between each check for a new point while holding down the trigger
        public float raycastInterval;

        public GameObject pointPrefab;
        public bool showDebugPoints;

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

            _inLinePickingMode = true;
            _currentLinePickingPoints.Clear();
            _linePointsToGameObjs.Clear();
            _continuousLinePickingCoroutine = StartCoroutine(ContinuousPicking());
        }

        public GameObject DrawPickedPointsAsLine(Vector3[] worldCoords, Transform radargramTransform)
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

            return lineObject;
        }

        // On trigger release, mark end of line picking
        private void OnLinePickEnd(InputAction.CallbackContext context)
        {
            if (!_inLinePickingMode || _continuousLinePickingCoroutine == null) return;
            
            if (_currentRadargram != null)
            {
                // Do cleanup here
                
                // for each set of line points, use get line picking points method to get line between the two points
                // = UVHelpers.GetLinePickingPoints(uvCoordinates, meshObj, _currentRadargram.name, raycastHit.normal, pixelsBetweenLinePoints);
                
                // at the end, concatenate these lines together
                
                // DrawPickedPointsAsLine(_currentLinePickingPoints.ToArray(), _currentRadargram);
            }
            
            EndLinePicking();
        }

        private void EndLinePicking()
        {
            _inLinePickingMode = false;
            StopCoroutine(ContinuousPicking());
            _continuousLinePickingCoroutine = null;
            _currentRadargram = null;
        }

        private void AddPoint(Vector3 pointToAdd, LinePickingPointInfo info) {
            GameObject markObj = Instantiate(pointPrefab, pointToAdd, info.hitRadargram.rotation);
            info.debugVisual = markObj;
            markObj.transform.parent = info.hitRadargram;
            markObj.SetActive(showDebugPoints);
            
            // when point is added, draw line from last point to current point if there is a last point
            if (_currentLinePickingPoints.Count > 0)
            {
                LinePickingPointInfo lastPoint = _currentLinePickingPoints.Last().Value;
                GameObject meshObj = _currentRadargram.GetChild(0).gameObject;
                
                // get UV of last line's endpoint
                Vector2 startUV = lastPoint.uvCoordinates;
                if (lastPoint.lineVisual)
                {
                    LineRenderer lineRenderer = lastPoint.lineVisual.GetComponent<LineRenderer>();
                    Vector3 lastPointOnPreviousLine = lineRenderer.GetPosition(lineRenderer.positionCount - 1);
                    startUV = UVHelpers.WorldToUV(lastPointOnPreviousLine, meshObj.GetComponent<MeshRenderer>().GetMesh(), meshObj.transform);
                }
                
                Debug.Log("Picking line from UV: " + startUV + " to " + info.uvCoordinates);

                Vector3[] worldCoords = UVHelpers.GetLinePickingPoints(startUV, info.uvCoordinates, meshObj, _currentRadargram.name, info.hitNormal, pixelsBetweenLinePoints);
                info.lineVisual = DrawPickedPointsAsLine(worldCoords, _currentRadargram);
            }
                
            _currentLinePickingPoints.Add(pointToAdd, info);
            _linePointsToGameObjs.Add(pointToAdd, markObj);
        }

        private void TryAddLastPoint(Vector3 pointToAdd, Vector3 lastPointPos, LinePickingPointInfo info)
        {
            float scaledInterval = linePickingHorizontalInterval / ScaleConstants.UNITY_TO_WORLD_SCALE;
            bool pointIsFarEnoughFromLastPoint = pointToAdd.x - lastPointPos.x >= scaledInterval;

            if (pointIsFarEnoughFromLastPoint)
                AddPoint(pointToAdd, info);
        }

        private void TryRemoveLastPoint(Vector3 raycastHitPos, Vector3 lastPointPos)
        {
            float scaledInterval = linePickingHorizontalInterval / ScaleConstants.UNITY_TO_WORLD_SCALE;
            bool pointIsFarEnoughFromLastPoint = raycastHitPos.x - lastPointPos.x <= scaledInterval;

            if (pointIsFarEnoughFromLastPoint)
            {
                _currentLinePickingPoints.TryGetValue(lastPointPos, out LinePickingPointInfo pointInfo);
                pointInfo?.debugVisual?.SetActive(false);
                pointInfo?.lineVisual?.SetActive(false);
                _currentLinePickingPoints.Remove(lastPointPos);
            }
        }
        
        IEnumerator ContinuousPicking()
        {
            while (_inLinePickingMode)
            {
                // Continually look
                if (rightControllerRayInteractor.TryGetCurrent3DRaycastHit(out var raycastHit))
                {
                    if (!_currentRadargram)
                    {
                        _currentRadargram = raycastHit.transform;
                    }
                    else if (_currentRadargram != raycastHit.transform)
                        EndLinePicking();
                    
                    bool isRadargramMesh = _currentRadargram.name.Contains("Data");
                    if (!isRadargramMesh) continue;
                    
                    // Get the mesh object that was hit
                    GameObject meshObj = _currentRadargram.GetChild(0).gameObject;

                    // Approximate UV coordinates from hit position
                    Vector2 uvCoordinates = UVHelpers.ApproximateUVFromHit(raycastHit.point, meshObj);
                    Vector3 potentialPoint = UVHelpers.GetPointOnMesh(uvCoordinates, meshObj, _currentRadargram.name);
                    
                    LinePickingPointInfo pointInfo = new LinePickingPointInfo();
                    pointInfo.hitRadargram = _currentRadargram;
                    pointInfo.uvCoordinates = uvCoordinates;
                    pointInfo.hitNormal = raycastHit.normal;

                    if (_currentLinePickingPoints.Count == 0)
                        AddPoint(potentialPoint, pointInfo);
                    else
                    {
                        float maxX = _currentLinePickingPoints.Keys.Max(v => v.x);
                        Vector3 lastPoint = _currentLinePickingPoints.Keys.Single((pos) => Mathf.Approximately(pos.x, maxX));
                        if (potentialPoint.x > lastPoint.x)
                            TryAddLastPoint(potentialPoint, lastPoint, pointInfo);
                        else if (potentialPoint.x < lastPoint.x)
                            TryRemoveLastPoint(potentialPoint, lastPoint);
                    }
                    
                    // can i get the cross product of the ray and the mesh
                    // Vector3[] worldCoords = UVHelpers.GetLinePickingPoints(uvCoordinates, meshObj, _currentRadargram.name, raycastHit.normal, pixelsBetweenLinePoints);
                    // GameObject markObj = Instantiate(markObjPrefab, raycastHit.point, _currentRadargram.rotation);
                    // markObj.transform.parent = _currentRadargram;

                    // DrawPickedPointsAsLine(worldCoords, _currentRadargram);
                }

                yield return new WaitForSeconds(raycastInterval / 1000);
            }
        }
    }
}
