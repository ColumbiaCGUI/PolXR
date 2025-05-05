using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UniGLTF.MeshUtility;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace LinePicking
{
    public class PickLine : MonoBehaviour
    {
        [SerializeField] private InputActionReference linePickingTrigger;

        [SerializeField] private InputActionReference guidedLinePickingToggle;

        [SerializeField] private XRRayInteractor rightControllerRayInteractor;

        [SerializeField] private GameObject markObjPrefab;

        private ToggleLinePickingMode _toggleLinePickingMode;

        private bool _inLinePickingMode;
        
        private Coroutine _continuousLinePickingCoroutine;

        private Dictionary<Vector3, LinePickingPointInfo> _currentLinePickingPoints = new();
        
        private List<GameObject> _gameObjectsToCleanup = new();

        private Transform _currentRadargram;
        
        [Header("Customization")]
        
        //. The amount of pixels between each manually picked point.
        public int linePickingHorizontalInterval = 30;
        
        /// Corresponds to automatically generated line points (which fall in between manually picked points.
        /// The amount of pixels between each point on the line. Higher values will result in less accurate lines, but should generate them more quickly.
        public int pixelsBetweenLinePoints = 10;

        public Color lineColor = new(0.2f, 0.2f, 1f);

        /// during line picking: ms between each check for a new point while holding down the trigger
        public float raycastInterval;

        public GameObject pointPrefab;
        
        [Header("Debug")]
        
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
            
            guidedLinePickingToggle.action.started += OnGuidedLinePickToggle;
        }

        private void OnDisable()
        {
            linePickingTrigger.action.started -= OnLinePickStart;
            linePickingTrigger.action.canceled -= OnLinePickEnd;
            
            guidedLinePickingToggle.action.started -= OnGuidedLinePickToggle;
        }

        private void OnGuidedLinePickToggle(InputAction.CallbackContext context)
        {
            _toggleLinePickingMode.ToggleGuidedLinePicking();
        }

        // On trigger press, mark start of line picking
        private void OnLinePickStart(InputAction.CallbackContext context)
        {
            if (!_toggleLinePickingMode.isLinePickingEnabled) return;

            _inLinePickingMode = true;
            _currentLinePickingPoints.Clear();
            _continuousLinePickingCoroutine = StartCoroutine(ContinuousPicking());
        }

        private void CleanupLinePickingObjects()
        {
            GameObject lineParent = new GameObject("Polyline");
            lineParent.transform.SetParent(_currentRadargram, false);
            
            foreach (var linePickingPointInfoKvp in _currentLinePickingPoints)
            {
                var linePickingPointInfo = linePickingPointInfoKvp.Value;
                Destroy(linePickingPointInfo.DebugVisual);
                linePickingPointInfo.DebugVisual = null;

                if (linePickingPointInfo.LineVisual)
                    linePickingPointInfo.LineVisual.transform.SetParent(lineParent.transform);
            }
        }
        
        // On trigger release, mark end of line picking
        private void OnLinePickEnd(InputAction.CallbackContext context)
        {
            if (!_inLinePickingMode || _continuousLinePickingCoroutine == null) return;
            
            // If line picking operation was valid, tidy up all the GameObjects we created
            if (_currentRadargram != null)
                CleanupLinePickingObjects();
            
            foreach (var obj in _gameObjectsToCleanup)
                Destroy(obj);
            _gameObjectsToCleanup.Clear();
            
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
            GameObject markObj = Instantiate(pointPrefab, pointToAdd, info.HitRadargram.rotation);
            info.DebugVisual = markObj;
            markObj.transform.parent = info.HitRadargram;
            markObj.SetActive(showDebugPoints);
            
            // when point is added, draw line from last point to current point if there is a last point
            if (_currentLinePickingPoints.Count > 0)
            {
                LinePickingPointInfo lastPoint = _currentLinePickingPoints.Last().Value;
                GameObject meshObj = _currentRadargram.GetChild(0).gameObject;
                
                // get UV of last line's endpoint
                Vector3 lastPointWorld = lastPoint.Point;
                if (lastPoint.LineVisual)
                {
                    LineRenderer lineRenderer = lastPoint.LineVisual.GetComponent<LineRenderer>();
                    lastPointWorld = lineRenderer.GetPosition(lineRenderer.positionCount - 1);
                }
                
                if (_toggleLinePickingMode.isGuidedLinePickingEnabled)
                {
                    Vector2 startUV = lastPoint.LineVisual ? CoordinateUtils.WorldToUV(lastPointWorld, meshObj.GetComponent<MeshRenderer>().GetMesh(), meshObj.transform) : lastPoint.UVCoordinates;
                    Vector3[] worldCoords = LineGeneration.GetGuidedLinePickingPoints(startUV, info.UVCoordinates, meshObj, _currentRadargram.name, info.HitNormal, pixelsBetweenLinePoints);
                    info.LineVisual = LinePickUtils.DrawPickedPointsAsLine(worldCoords, _currentRadargram, lineColor);
                }
                else
                {
                    Vector3 startPoint = lastPoint.LineVisual ? lastPointWorld : _currentRadargram.InverseTransformPoint(lastPointWorld);
                    Vector3 endPoint = _currentRadargram.InverseTransformPoint(pointToAdd);

                    Vector3[] points = LineGeneration.GetUnguidedLinePickingPoints(startPoint, endPoint, meshObj, pixelsBetweenLinePoints);
                    info.LineVisual = LinePickUtils.DrawPickedPointsAsLine(points, _currentRadargram, lineColor);
                }
            }
                
            _currentLinePickingPoints.Add(pointToAdd, info);
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
                
                DeactivateLineMarkObject(pointInfo?.DebugVisual);
                DeactivateLineMarkObject(pointInfo?.LineVisual);
                _currentLinePickingPoints.Remove(lastPointPos);
            }
        }
       
        private void DeactivateLineMarkObject(GameObject obj)
        {
            if (!obj) return;
            
            obj.SetActive(false);
            _gameObjectsToCleanup.Add(obj);
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
                    Vector2 uvCoordinates = RadargramMeshUtils.ApproximateUVFromHit(raycastHit.point, meshObj);
                    Vector3 potentialPoint = RadargramMeshUtils.GetPointOnRadargramMesh(uvCoordinates, meshObj, _currentRadargram.name);
                    
                    LinePickingPointInfo pointInfo = new LinePickingPointInfo();
                    pointInfo.Point = potentialPoint;
                    pointInfo.HitRadargram = _currentRadargram;
                    pointInfo.UVCoordinates = uvCoordinates;
                    pointInfo.HitNormal = raycastHit.normal;

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
                }

                yield return new WaitForSeconds(raycastInterval / 1000);
            }
        }
    }
}
