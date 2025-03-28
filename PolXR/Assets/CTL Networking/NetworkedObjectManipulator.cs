using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Fusion;
using Fusion.Sockets;
using System;

[RequireComponent(typeof(NetworkObject))]
public class NetworkedObjectManipulator : XRGrabInteractable
{
    private NetworkObject _networkObject;
    private NetworkTransform _networkTransform;
    private Rigidbody _rigidbody;
    private NetworkRunner _runner;

    // Color properties for visual feedback
    [Header("Visual Feedback")]
    [SerializeField] private Color _defaultColor = Color.white;
    [SerializeField] private Color _hoverColor = Color.yellow;
    [SerializeField] private Color _grabColor = Color.green;

    private Renderer _renderer;
    private Material _material;
    private bool _isGrabbed = false;

    protected override void Awake()
    {
        base.Awake();

        _networkObject = GetComponent<NetworkObject>();
        _networkTransform = GetComponent<NetworkTransform>();
        _rigidbody = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();

        if (_renderer != null)
        {
            // Create a material instance to avoid affecting shared materials
            _material = new Material(_renderer.material);
            _renderer.material = _material;
            _material.color = _defaultColor;
        }

        // Get network runner reference when spawned
        if (_networkObject != null)
        {
            _runner = _networkObject.Runner;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // No need to manually subscribe to events when overriding the methods
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // No need to manually unsubscribe from events when overriding the methods
    }

    protected override void OnHoverEntered(HoverEnterEventArgs args)
    {
        base.OnHoverEntered(args);
        UpdateVisualState(true, _isGrabbed);
    }

    protected override void OnHoverExited(HoverExitEventArgs args)
    {
        base.OnHoverExited(args);
        UpdateVisualState(false, _isGrabbed);
    }

    protected IEnumerator WaitForStateAuthority()
    {
        while (!_networkObject.HasStateAuthority)
        {
            _networkObject.RequestStateAuthority();
            yield return null;
        }
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        StartCoroutine(WaitForStateAuthority());

        base.OnSelectEntered(args);
        _isGrabbed = true;
        UpdateVisualState(false, true);

        // Ensure physics is kinematic when grabbed
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true;
        }

        // For Fusion objects with OverrideStateAuthority,
        // NetworkTransform will handle position synchronization automatically
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        _networkObject.ReleaseStateAuthoirty();

        base.OnSelectExited(args);
        _isGrabbed = false;
        UpdateVisualState(false, false);

        // Keep the rigidbody kinematic to prevent it from falling
        if (_rigidbody != null)
        {
            // Keep isKinematic true so the object stays in place
            _rigidbody.isKinematic = true;


        }

        // NetworkTransform will continue handling position synchronization
    }

    private void UpdateVisualState(bool isHovered, bool isGrabbed)
    {
        if (_material != null)
        {
            if (isGrabbed)
            {
                _material.color = _grabColor;
            }
            else if (isHovered)
            {
                _material.color = _hoverColor;
            }
            else
            {
                _material.color = _defaultColor;
            }
        }
    }

    // Override the ProcessInteractable method to ensure smooth movement
    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        // If object is grabbed and we're in the correct update phase
        if (_isGrabbed && updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            // NetworkTransform automatically handles synchronization
            // We don't need to manually set networked position/rotation
        }
    }
}
