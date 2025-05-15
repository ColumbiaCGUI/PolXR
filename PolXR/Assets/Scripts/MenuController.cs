using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;


// original working code without grabbing functionality
public class MenuController : MonoBehaviour
{
     [SerializeField] public Transform user;
     public GameObject mainMenu;
     public GameObject mainMenuTitle;
     public GameObject radarMenu;
     public GameObject radarMenuTitle;
     public GameObject mapMenu;
     public GameObject mapMenuTitle;

     private Vector3 offset=new Vector3(0.0f,0.0f,0.0f);
    float radius= 0.15f;
    

    void Start()
    {
        radarMenuTitle.SetActive(false);
        mapMenuTitle.SetActive(false);
        mainMenuTitle.SetActive(false);
    }
    void Update()
    {

        Vector3 waistPosition= user.position + Vector3.up * offset.y;
        Vector3 cameraForward= user.forward;
        cameraForward.y=0;
        cameraForward.Normalize();

        transform.position=waistPosition + cameraForward*radius;
        Vector3 directionToCamera= user.position-transform.position;
        directionToCamera.y=0;
        transform.rotation=Quaternion.LookRotation(-directionToCamera, Vector3.up);
    }

    public void ToggleMain(){
        mainMenu.SetActive(!mainMenu.activeSelf);
        mainMenuTitle.SetActive(!mainMenuTitle.activeSelf);
    }

    public void ToggleRadar(){
        radarMenu.SetActive(!radarMenu.activeSelf);
        radarMenuTitle.SetActive(!radarMenuTitle.activeSelf);
    }
    public void ToggleMap(){
        mapMenu.SetActive(!mapMenu.activeSelf);
        mapMenuTitle.SetActive(!mapMenuTitle.activeSelf);
    }
}


/*
// doesn't work currently, needs debugging
public class MenuController : MonoBehaviour
{
    [Header("User Tracking")]
    [SerializeField] public Transform user;
    [SerializeField] private float _radius = 0.15f;
    private Vector3 _offset = new Vector3(0.0f, 0.0f, 0.0f);

    [Header("Submenus")]
    public GameObject mainMenu;
    public GameObject radarMenu;
    public GameObject mapMenu;
   
    void Update()
    {
        // Original follow behavior
        Vector3 waistPosition = user.position + Vector3.up * _offset.y;
        Vector3 cameraForward = user.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        transform.position = waistPosition + cameraForward * _radius;
        transform.rotation = Quaternion.LookRotation(-new Vector3(user.position.x, transform.position.y, user.position.z) + transform.position);
    }

    // Modified toggle methods to reset positions
    public void ToggleMain()
    {
        mainMenu.SetActive(!mainMenu.activeSelf);
        if (mainMenu.activeSelf && mainMenu.TryGetComponent<MenuDragController>(out var draggable))
            draggable.ResetPosition();
    }

    public void ToggleRadar()
    {
        radarMenu.SetActive(!radarMenu.activeSelf);
        if (radarMenu.activeSelf && radarMenu.TryGetComponent<MenuDragController>(out var draggable))
            draggable.ResetPosition();
    }

    public void ToggleMap()
    {
        mapMenu.SetActive(!mapMenu.activeSelf);
        if (mapMenu.activeSelf && mapMenu.TryGetComponent<MenuDragController>(out var draggable))
            draggable.ResetPosition();
    }
}

*/