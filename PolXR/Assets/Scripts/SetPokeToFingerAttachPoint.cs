using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SetPokeToFingerAttachPoint : MonoBehaviour
{
    public Transform PokeAttachPoint;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor xrPokeInteractor;
    void Start()
    {
        xrPokeInteractor = transform.parent.parent.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor>();
        SetPokeAttachPoint();
    }

    void SetPokeAttachPoint()
    {
        xrPokeInteractor.attachTransform = PokeAttachPoint;
    }


}
