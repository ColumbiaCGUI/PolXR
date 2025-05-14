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
    private GameObject markerA;
    private GameObject markerB;

    void OnEnable()
    {
        placeAction.action.performed += OnPlacePressed;
    }

    void OnDisable()
    {
        placeAction.action.performed -= OnPlacePressed;
    }

    private void OnPlacePressed(InputAction.CallbackContext context)
    {
        if (!MeasureModeController.IsMeasureModeActive)
            return;

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            Vector3 hitPoint = hit.point;

            // Try to auto-set active flightline if nearby
            Collider[] nearby = Physics.OverlapSphere(hitPoint, 0.1f);
            foreach (var col in nearby)
            {
                if (col.name.StartsWith("Flightline"))
                {
                    LineRenderer lr = col.GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        measurementManager.SetFlightline(lr);
                        Debug.Log("[XRMeasurePlacer] Flightline automatically set from hit.");
                        break;
                    }
                }
            }

            if (pointA == null)
            {
                pointA = hitPoint;
                markerA = Instantiate(markerPrefab, hitPoint, Quaternion.identity);
            }
            else if (pointB == null)
            {
                pointB = hitPoint;
                markerB = Instantiate(markerPrefab, hitPoint, Quaternion.identity);
                measurementManager.SetMeasurementPoints(pointA.Value, pointB.Value);
            }
            else
            {
                // Reset to start a new measurement
                Destroy(markerA);
                Destroy(markerB);
                pointA = hitPoint;
                pointB = null;
                markerA = Instantiate(markerPrefab, hitPoint, Quaternion.identity);
            }
        }
    }
}
