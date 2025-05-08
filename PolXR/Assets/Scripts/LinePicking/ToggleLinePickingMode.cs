using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace LinePicking
{
    public class ToggleLinePickingMode : MonoBehaviour
    {
        public bool isLinePickingEnabled = false;
        public bool isGuidedLinePickingEnabled = true;
    
        [SerializeField] private InputActionReference toggleLinePickingButton;
    
        [SerializeField] private XRInteractorLineVisual leftControllerLineVisual;
        [SerializeField] private XRInteractorLineVisual rightControllerLineVisual;

        private Gradient _initialRightControllerValidLineGradient;
        
        public Gradient guidedLinePickingColorGradient; 
        public Gradient unguidedLinePickingColorGradient; 
        
        private float _initialLeftControllerLineBendRatio;
        private float _initialRightControllerLineBendRatio;

        private void Start()
        {
            _initialLeftControllerLineBendRatio = leftControllerLineVisual.lineBendRatio;
            _initialRightControllerLineBendRatio = rightControllerLineVisual.lineBendRatio;

            _initialRightControllerValidLineGradient = rightControllerLineVisual.validColorGradient;
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
            
            UpdateRayInteractorGradient();
        }

        private void DisableLinePicking()
        {
            isLinePickingEnabled = false;
        
            leftControllerLineVisual.lineBendRatio = _initialLeftControllerLineBendRatio;
            rightControllerLineVisual.lineBendRatio = _initialRightControllerLineBendRatio;
            
            UpdateRayInteractorGradient();
        }

        public void ToggleGuidedLinePicking()
        {
            if (!isLinePickingEnabled) return;
            
            if (isGuidedLinePickingEnabled)
                DisableGuidedLinePicking();
            else
                EnableGuidedLinePicking();
        }

        private void UpdateRayInteractorGradient()
        {
            if (!isLinePickingEnabled)
            {
                rightControllerLineVisual.validColorGradient = _initialRightControllerValidLineGradient;
                return;
            }
            
            rightControllerLineVisual.validColorGradient = isGuidedLinePickingEnabled ? unguidedLinePickingColorGradient : guidedLinePickingColorGradient;
        }

        private void EnableGuidedLinePicking()
        {
            isGuidedLinePickingEnabled = true;
            UpdateRayInteractorGradient();
        }
        
        private void DisableGuidedLinePicking()
        {
            isGuidedLinePickingEnabled = false;
            UpdateRayInteractorGradient();
        }
    }
}
