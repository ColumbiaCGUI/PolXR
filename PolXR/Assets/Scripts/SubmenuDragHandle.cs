using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SubmenuDragHandle : XRBaseInteractable
{
    public Transform menuToDrag;
    public float dragRange = 0.5f;

    private XRBaseInteractor grabbingInteractor;
    private Vector3 grabOffset;
    private Vector3 originalLocalPosition;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        grabbingInteractor = args.interactorObject.transform.GetComponent<XRBaseInteractor>();
        originalLocalPosition = menuToDrag.position;
        grabOffset = menuToDrag.position - grabbingInteractor.transform.position;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        grabbingInteractor = null;
    }

    void Update()
    {

        if (grabbingInteractor)
        {
            Debug.Log(grabbingInteractor.name);
            Vector3 newWorldPos = grabbingInteractor.transform.position + grabOffset;

            // Clamp in world space based on distance from original position
            Vector3 offsetFromOriginal = newWorldPos - menuToDrag.position;
            Vector3 totalDisplacement = (newWorldPos - menuToDrag.position) + (menuToDrag.position - originalLocalPosition);

            if (totalDisplacement.magnitude <= dragRange)
            {
                menuToDrag.position = newWorldPos;
            }
            else
            {
                Vector3 clampedDisplacement = Vector3.ClampMagnitude(totalDisplacement, dragRange);
                menuToDrag.position = originalLocalPosition + clampedDisplacement;
            }
            /*
            Vector3 newWorldPos = grabbingInteractor.transform.position + grabOffset;
            Vector3 newLocalPos = menuToDrag.parent.InverseTransformPoint(newWorldPos);
            Vector3 clampedLocal = Vector3.ClampMagnitude(newLocalPos - originalLocalPosition, dragRange) + originalLocalPosition;
            menuToDrag.localPosition = clampedLocal;
            */
        }
    }
}