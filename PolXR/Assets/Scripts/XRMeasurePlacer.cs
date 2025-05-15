using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class XRMeasurePlacer : MonoBehaviour
{
    public XRRayInteractor rayInteractor;
    public InputActionReference placeAction;
    public MeasurementManager measurementManager;
    public GameObject markerPrefab;

    private Vector3? pointA = null;
    private Vector3? pointB = null;
    private GameObject previewDot;

    void OnEnable()
    {
        placeAction.action.performed += OnPlacePressed;
    }

    void OnDisable()
    {
        placeAction.action.performed -= OnPlacePressed;
    }

    public void ResetMeasurement()
    {
        pointA = null;
        pointB = null;
        if (previewDot != null)
        {
            Destroy(previewDot);
            previewDot = null;
        }
    }

    private void OnPlacePressed(InputAction.CallbackContext context)
    {
        if (!MeasureModeController.IsMeasureModeActive)
            return;

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            Vector3 hitPoint = hit.point;
            Collider[] nearby = Physics.OverlapSphere(hitPoint, 0.1f);

            foreach (var col in nearby)
            {
                if (col.name.StartsWith("Flightline"))
                {
                    LineRenderer lr = col.GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        measurementManager.SetFlightline(lr);
                        break;
                    }
                }
            }

            if (pointA == null)
            {
                pointA = hitPoint;

                if (previewDot != null) Destroy(previewDot);
                previewDot = Instantiate(markerPrefab, hitPoint, Quaternion.identity);
            }
            else if (pointB == null)
            {
                pointB = hitPoint;
                measurementManager.SetMeasurementPoints(pointA.Value, pointB.Value);
                if (previewDot != null)
                {
                    Destroy(previewDot);
                    previewDot = null;
                }
            }
            else
            {
                pointA = hitPoint;
                pointB = null;
                if (previewDot != null) Destroy(previewDot);
                previewDot = Instantiate(markerPrefab, hitPoint, Quaternion.identity);
            }
        }
    }
}
