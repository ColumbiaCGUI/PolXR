using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FlightlineInteractions : MonoBehaviour
{
    //private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interact;
    private Renderer meshRenderer;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    
    void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        Collider collider = GetComponent<Collider>();
        
        if(interactable != null && collider != null)
        {
            interactable.colliders.Add(collider);
            interactable.selectEntered.AddListener(OnSelectEntered);
            //interactable.selectExited.AddListener(OnSelectExited);
            Debug.Log($"{gameObject.name}: Adding interaction listeners to {gameObject.name}.");
        }
        else
        {
            Debug.LogError("nothing added RAHH");
        }
    }
    /*
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Flightline {gameObject.name} was poked by {other.name}");
            HighlightFlightline();
        }
    }
    */
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        HighlightFlightline();
    }
    public void HighlightFlightline()
    {
        if(meshRenderer != null)
        {
            Color highlight = Color.black;
            ColorUtility.TryParseHtmlString("#8BF394", out highlight);
            meshRenderer.material.color = highlight;
            Transform parent = transform.parent;
            foreach(Transform child in parent)
            {
                if(child.name.StartsWith("Data"))
                {
                    child.gameObject.SetActive(true);
                }
            }
        }
        else {
            Debug.LogError("NOOOO");
        }
    }

    public void OnFlightlineDeselected(SelectExitEventArgs args)
    {
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            //interact.selectEntered.RemoveListener(OnFlightlineSelected);
            interactable.selectExited.RemoveListener(OnFlightlineDeselected);
        }
    }
}