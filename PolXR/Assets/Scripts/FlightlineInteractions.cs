using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FlightlineInteractions : MonoBehaviour
{
    private Renderer meshRenderer;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private bool isAlreadySelected = false;
    private GameObject radargram;
    
    void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        Collider collider = GetComponent<Collider>();
        Transform parent = transform.parent;
        foreach(Transform child in parent)
        {
            if(child.name.StartsWith("Data"))
            {
                radargram = child.gameObject;
            }
        }
        
        if(interactable != null && collider != null)
        {
            interactable.colliders.Add(collider);
            //to detect when users interact with flightline
            interactable.selectEntered.AddListener(OnSelectEntered);
        }
        else
        {
            Debug.LogError("nothing added RAHH");
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if(!isAlreadySelected)
        {
            SelectFlightline();
        }
        else 
        {
            DeselectFlightline();
        }
    }
    public void SelectFlightline()
    {
        if(meshRenderer != null)
        {
            Color highlight = Color.black;
            ColorUtility.TryParseHtmlString("#8BF394", out highlight);
            meshRenderer.material.color = highlight;
            radargram.SetActive(true);
            isAlreadySelected = true;
        }
        else 
        {
            Debug.LogError("NO renderer found for flightline");
        }
    }

    public void DeselectFlightline()
    {
        meshRenderer.material.color = Color.black;
        radargram.SetActive(false);
        isAlreadySelected = false;
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }
}