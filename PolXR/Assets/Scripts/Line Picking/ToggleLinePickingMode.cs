using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class ToggleLinePickingMode : MonoBehaviour
{
    public bool isLinePickingEnabled = false;
    
    [SerializeField] private InputActionReference toggleLinePickingButton;
    [SerializeField] private XRInteractorLineVisual interactorLineVisual;

    private float _initialLineBendRatio;

    private void Start()
    {
        _initialLineBendRatio = interactorLineVisual.lineBendRatio;
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
        interactorLineVisual.lineBendRatio = 1.0f;
    }

    private void DisableLinePicking()
    {
        isLinePickingEnabled = false;
        
        interactorLineVisual.lineBendRatio = _initialLineBendRatio;
    }
}
