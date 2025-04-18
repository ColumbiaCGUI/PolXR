using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

public class ToggleLinePickingMode : MonoBehaviour
{
    public bool isLinePickingEnabled = false;
    
    [SerializeField] private InputActionReference toggleLinePickingButton;
    
    [SerializeField] private XRInteractorLineVisual leftControllerLineVisual;
    [SerializeField] private XRInteractorLineVisual rightControllerLineVisual;
    
    private float _initialLeftControllerLineBendRatio;
    private float _initialRightControllerLineBendRatio;

    private void Start()
    {
        _initialLeftControllerLineBendRatio = leftControllerLineVisual.lineBendRatio;
        _initialRightControllerLineBendRatio = rightControllerLineVisual.lineBendRatio;
    }
    
    private void OnEnable()
    {
        toggleLinePickingButton.action.started += OnLinePickingButtonPressed;
    }

    private void OnDisable()
    {
        toggleLinePickingButton.action.started -= OnLinePickingButtonPressed;
    }

    private void OnLinePickingButtonPressed(InputAction.CallbackContext context)
    {
        if (isLinePickingEnabled)
        {
            DisableLinePicking();
        }
        else
        {
            EnableLinePicking();
        }
    }

    private void EnableLinePicking()
    {
        isLinePickingEnabled = true;

        // Make the ray interactor line straight to make line picking feel as precise as possible
        leftControllerLineVisual.lineBendRatio = 1.0f;
        rightControllerLineVisual.lineBendRatio = 1.0f;
    }

    private void DisableLinePicking()
    {
        isLinePickingEnabled = false;
        
        leftControllerLineVisual.lineBendRatio = _initialLeftControllerLineBendRatio;
        rightControllerLineVisual.lineBendRatio = _initialRightControllerLineBendRatio;
    }
}
