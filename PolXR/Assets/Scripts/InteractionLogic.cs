using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class InteractionLogic : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    private GameObject radargram; // Cached radargram reference

    private void Start()
    {
        // Dynamically find the radargram sibling (assuming shared parent)
        Transform parent = transform.parent;
        if (parent != null)
        {
            foreach (Transform sibling in parent)
            {
                if (sibling.CompareTag("Radargram"))
                {
                    radargram = sibling.gameObject;
                    break;
                }
            }
        }
    }

    // Triggered when the object is selected (via controller "Select" or hand poke)
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (radargram != null)
        {
            bool isActive = radargram.activeSelf;
            radargram.SetActive(!isActive); // Toggle visibility
            Debug.Log($"Radargram {radargram.name} visibility toggled to {!isActive}");
        }
        else
        {
            Debug.LogWarning("No radargram associated with this flightline.");
        }
    }
}
