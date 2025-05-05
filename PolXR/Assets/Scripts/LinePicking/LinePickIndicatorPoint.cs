using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace LinePicking
{
    public class LinePickIndicatorPoint : MonoBehaviour
    {
        [SerializeField] private GameObject[] indicatorMeshes;

        [SerializeField] private GameObject offsetParent;

        [SerializeField] private XRRayInteractor rightControllerRayInteractor;

        [SerializeField] private ToggleLinePickingMode linePickingState;

        private void HideIndicator()
        {
            foreach (GameObject indicatorMeshObj in indicatorMeshes)
            {
                indicatorMeshObj.gameObject.SetActive(false);
            }
        }
        
        private void ShowIndicator()
        {
            foreach (GameObject indicatorMeshObj in indicatorMeshes)
            {
                indicatorMeshObj.gameObject.SetActive(true);
            }
        }
        
        private void Update()
        {
            if (linePickingState.isLinePickingEnabled && rightControllerRayInteractor.TryGetCurrent3DRaycastHit(out var raycastHit))
            {
                if (!raycastHit.transform.name.Contains("Data"))
                {
                    HideIndicator();
                    return;
                }

                offsetParent.transform.position = raycastHit.point;
                offsetParent.transform.rotation = Quaternion.LookRotation(raycastHit.normal);
                ShowIndicator();
                return;
            }
            
            HideIndicator();
        }
    }
}