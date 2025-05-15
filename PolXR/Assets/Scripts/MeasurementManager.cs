using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MeasurementManager : MonoBehaviour
{
    public GameObject linePrefab;
    public GameObject distanceTextPrefab;
    public GameObject dotPrefab;

    private GameObject line;
    private GameObject distanceText;
    private GameObject dotA;
    private GameObject dotB;

    private LineRenderer lineRenderer;
    private TextMeshProUGUI distanceTextUI;
    private LineRenderer activeFlightline;

    public enum DistanceUnit { Meters, Kilometers }
    public DistanceUnit currentUnit = DistanceUnit.Meters;

    public GameObject tickMarkPrefab;
    public float tickSpacing = 0.5f; // in Unity units (adjust for density)
    private List<GameObject> activeTicks = new();

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            ToggleUnit();
        }

        if (distanceText != null)
        {
            Vector3 lookDirection = Camera.main.transform.position - distanceText.transform.position;
            lookDirection.y = 0;
            distanceText.transform.rotation = Quaternion.LookRotation(lookDirection);
            distanceText.transform.Rotate(0f, 180f, 0f);
        }
    }

    public void SetFlightline(LineRenderer lr)
    {
        activeFlightline = lr;
        Debug.Log($"Flightline set: {lr.name} with {lr.positionCount} points.");
    }

    public void SetMeasurementPoints(Vector3 a, Vector3 b)
    {
        if (lineRenderer != null)
            lineRenderer.positionCount = 0;

        if (dotA == null) dotA = Instantiate(dotPrefab);
        if (dotB == null) dotB = Instantiate(dotPrefab);

        dotA.transform.position = a;
        dotB.transform.position = b;

        bool onFlightlineA = IsSnappedToFlightline(a);
        bool onFlightlineB = IsSnappedToFlightline(b);
        bool onRadargramA = IsSnappedToRadargram(a);
        bool onRadargramB = IsSnappedToRadargram(b);

        Debug.Log($"[SetMeasurementPoints] onFlightlineA={onFlightlineA}, onFlightlineB={onFlightlineB}, onRadargramA={onRadargramA}, onRadargramB={onRadargramB}");

        // --- Case 1: Draw curved line along flightline ---
        if (onFlightlineA && onFlightlineB && activeFlightline != null && activeFlightline.positionCount > 1)
        {
            int indexA = GetClosestIndexOnLine(activeFlightline, a);
            int indexB = GetClosestIndexOnLine(activeFlightline, b);

            if (indexA > indexB)
            {
                (indexA, indexB) = (indexB, indexA);
                (a, b) = (b, a);
            }

            List<Vector3> curve = new();
            for (int i = indexA; i <= indexB; i++)
                curve.Add(activeFlightline.GetPosition(i));


            SetLineAndLabel(curve);
            return;
        }

        // --- Case 2: Radargram mesh curved mode ---
        if (onRadargramA && onRadargramB)
        {
            Transform meshA = FindRadargramMesh(a);
            Transform meshB = FindRadargramMesh(b);

            if (meshA != null && meshB != null && meshA == meshB)
            {
                List<Vector3> path = SampleRadargramMesh(meshA, a, b);
                SetLineAndLabel(path);
                return;
            }
        }

        // --- Case 3: Default straight line ---
        SetLineAndLabel(new List<Vector3>() { a, b });
    }

    private void CreateTickMarks(List<Vector3> path)
    {
        foreach (var tick in activeTicks)
            Destroy(tick);
        activeTicks.Clear();

        float accumulated = 0f;
        Vector3 previous = path[0];

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 current = path[i];
            float segmentLength = Vector3.Distance(previous, current);
            Vector3 dir = (current - previous).normalized;

            while (accumulated + tickSpacing <= segmentLength)
            {
                accumulated += tickSpacing;
                Vector3 tickPos = previous + dir * accumulated;

                GameObject tick = Instantiate(tickMarkPrefab, tickPos, Quaternion.identity);
                tick.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                tick.GetComponent<Renderer>().material.color = Color.white;

                tick.transform.LookAt(current); 
                activeTicks.Add(tick);
            }

            accumulated = (accumulated + tickSpacing > segmentLength)
                          ? accumulated + tickSpacing - segmentLength
                          : 0f;

            previous = current;
        }
    }

    private void SetLineAndLabel(List<Vector3> path)
    {
        if (path == null || path.Count < 2)
            return;

        if (line == null)
        {
            line = Instantiate(linePrefab);
            line.name = "MeasurementLine";
            lineRenderer = line.GetComponentInChildren<LineRenderer>();
            lineRenderer.material.color = Color.white;
        }

        if (distanceText == null)
        {
            distanceText = Instantiate(distanceTextPrefab);
            distanceText.name = "DistanceText";
            distanceTextUI = distanceText.transform.Find("Canvas/DistanceLabel").GetComponent<TextMeshProUGUI>();
        }

        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            path[i] += Vector3.up * 0.01f; // subtle offset to avoid z-fighting
        }
        lineRenderer.SetPositions(path.ToArray());

        float distance = 0f;
        for (int i = 1; i < path.Count; i++)
            distance += Vector3.Distance(path[i - 1], path[i]);
        distance *= 10000f;

        if (currentUnit == DistanceUnit.Meters)
            distanceTextUI.text = $"{distance:F2} meters";
        else
            distanceTextUI.text = $"{distance / 1000f:F2} km";

        Vector3 midpoint = GetMidpointAlongCurve(path);
        distanceText.transform.position = midpoint + new Vector3(0, 0.02f, 0);

        Vector3 lookDirection = Camera.main.transform.position - distanceText.transform.position;
        lookDirection.y = 0;
        distanceText.transform.rotation = Quaternion.LookRotation(lookDirection);
        distanceText.transform.Rotate(0f, 180f, 0f);

        if (dotA == null) dotA = Instantiate(dotPrefab);
        if (dotB == null) dotB = Instantiate(dotPrefab);

        dotA.transform.position = path[0];
        dotB.transform.position = path[^1];

        CreateTickMarks(path);
    }

    private Vector3 GetMidpointAlongCurve(List<Vector3> points)
    {
        if (points == null || points.Count < 2)
            return Vector3.zero;

        float totalDist = 0f;
        for (int i = 1; i < points.Count; i++)
            totalDist += Vector3.Distance(points[i - 1], points[i]);

        float halfDist = totalDist / 2f;
        float runningDist = 0f;

        for (int i = 1; i < points.Count; i++)
        {
            float segment = Vector3.Distance(points[i - 1], points[i]);
            runningDist += segment;
            if (runningDist >= halfDist)
                return (points[i - 1] + points[i]) / 2f;
        }

        return (points[0] + points[points.Count - 1]) / 2f;
    }

    private bool IsSnappedToRadargram(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 0.07f);
        foreach (var hit in hits)
        {
            if (hit.gameObject.name == "mesh" &&
                hit.transform.parent != null &&
                hit.transform.parent.name.StartsWith("Data_"))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsSnappedToFlightline(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 0.05f);
        foreach (var hit in hits)
        {
            if (hit.name.StartsWith("Flightline") && hit.GetComponent<LineRenderer>() != null)
            {
                Transform parent = hit.transform.parent;
                if (parent == null || !parent.name.StartsWith("Data_"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private Transform FindRadargramMesh(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, 0.07f);
        foreach (var hit in hits)
        {
            if (hit.gameObject.name == "mesh" &&
                hit.transform.parent != null &&
                hit.transform.parent.name.StartsWith("Data_"))
            {
                return hit.transform;
            }
        }
        return null;
    }

    private List<Vector3> SampleRadargramMesh(Transform radarMesh, Vector3 start, Vector3 end, int samples = 200)
    {
        List<Vector3> points = new List<Vector3>();
        int radarLayer = LayerMask.GetMask("Radargram");
        Vector3 localUp = radarMesh.up;

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 worldPos = Vector3.Lerp(start, end, t);
            Vector3 rayOrigin = worldPos - localUp * 2f;
            Vector3 rayDir = localUp;

            if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, 5f, radarLayer))
            {
                points.Add(hit.point + hit.normal * 0.01f);
            }
            else
            {
                points.Add(worldPos);
            }
        }
        return points;
    }

    private int GetClosestIndexOnLine(LineRenderer line, Vector3 point)
    {
        float minDist = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < line.positionCount; i++)
        {
            float dist = Vector3.Distance(point, line.GetPosition(i));
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    public void ToggleUnit()
    {
        currentUnit = currentUnit == DistanceUnit.Meters ? DistanceUnit.Kilometers : DistanceUnit.Meters;
        // Reapply label with current path, if it exists
        if (lineRenderer != null && lineRenderer.positionCount > 1)
        {
            List<Vector3> currentPath = new();
            for (int i = 0; i < lineRenderer.positionCount; i++)
                currentPath.Add(lineRenderer.GetPosition(i));

            SetLineAndLabel(currentPath);
        }

    }

}
