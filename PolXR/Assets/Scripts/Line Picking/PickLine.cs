using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

public class PickLine : MonoBehaviour
{
    [SerializeField] private InputActionReference linePickingTrigger;

    [SerializeField] private XRRayInteractor rightControllerRayInteractor;

    [SerializeField] private GameObject markObjPrefab;
    
    private ToggleLinePickingMode _toggleLinePickingMode;

    private bool _isPickingLine;

    private void Start()
    {
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
        
        // mark start point

        // try to find radargram with a raycast
        if (rightControllerRayInteractor.TryGetCurrent3DRaycastHit(out var raycastHit))
        {
            Vector3 radargramPoint = raycastHit.point; // the coordinate that the ray hits
            Debug.Log("point on radargram: " + radargramPoint);
            
            _isPickingLine = true;
            
            // get local position of hit point relative to the radargram
            Transform hitRadargram = raycastHit.transform;
            // Vector3 localPosition = hitRadargram.InverseTransformPoint(raycastHit.point);

            // set the mark object transform to the hit point
            GameObject markObj = Instantiate(markObjPrefab, raycastHit.point, hitRadargram.rotation);
            markObj.transform.parent = hitRadargram;
        }
    }

    // On trigger release, mark end of line picking
    private void OnLinePickEnd(InputAction.CallbackContext context)
    {
        if (!_isPickingLine) return;
            
        // mark end point
        Debug.Log("Line pick end");
    }

}
