using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using UnityEngine.InputSystem;

public class HomeMenuController : MonoBehaviour
{
    [SerializeField] private Transform user;
    [SerializeField] private Transform userCamera;
    private float distance = 0.5f;
    [SerializeField] private Transform sceneDropdown; // DEM dropdown container
    [SerializeField] private GameObject dropdownPrefab; // Flightline dropdown prefab
    [SerializeField] private Transform dropdownContainer; // Parent for flightline dropdowns
    [SerializeField] private Button addButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private RectTransform initialDropdown;
    [SerializeField] private InputActionReference toggleHomeMenuButton;

    private Vector3 startPosition;
    private List<GameObject> dropdownList = new List<GameObject>();
    private float verticalSpacing = -9f;
    private List<string> cachedFlightlineOptions = new List<string>();

    void Awake()
    {
        loadButton.onClick.AddListener(HandleLoad);
    }

    void Start()
    {
        startPosition = initialDropdown.anchoredPosition;
        dropdownList.Add(initialDropdown.gameObject);

        loadButton.interactable = false;

        // Set initial "Loading..." for DEM
        TMP_Dropdown demDropdown = sceneDropdown.GetComponentInChildren<TMP_Dropdown>();
        if (demDropdown != null)
            SetLoadingState(demDropdown);

        // Set initial "Loading..." for first flightline
        TMP_Dropdown firstFlightline = initialDropdown.GetComponentInChildren<TMP_Dropdown>();
        if (firstFlightline != null)
            SetLoadingState(firstFlightline);

        Button initialDltBtn = initialDropdown.GetComponentInChildren<Button>();
        initialDltBtn.onClick.AddListener(() => DeleteDropdown(initialDropdown.gameObject));
        addButton.onClick.AddListener(AddDropdown);

        StartCoroutine(WaitAndPopulateDropdowns());
    }

    IEnumerator WaitAndPopulateDropdowns()
    {
        yield return new WaitUntil(() => DataLoader.Instance.copyComplete);

        // Identify if Editor or Android build... If/else statements tested !Application.Editor, Application.Android, and UNITY_ANDROID do not work as conditional IDs this recognizes
        #if UNITY_EDITOR

            string demPath = ("Assets/AppData/DEMs");
            string flightlinesPath = ("Assets/AppData/Flightlines");

        #else
            
            string demPath = Path.Combine(Application.persistentDataPath, "Assets", "AppData", "DEMs");
            string flightlinesPath = Path.Combine(Application.persistentDataPath, "Assets", "AppData", "Flightlines");

        #endif
        
        // Populate DEM dropdown
        TMP_Dropdown demDropdown = sceneDropdown.GetComponentInChildren<TMP_Dropdown>();
        if (demDropdown != null && Directory.Exists(demPath))
        {
            List<string> demDirs = new List<string>(Directory.GetDirectories(demPath));
            demDropdown.ClearOptions();
            List<TMP_Dropdown.OptionData> demOptions = new List<TMP_Dropdown.OptionData>();
            foreach (string dir in demDirs)
                demOptions.Add(new TMP_Dropdown.OptionData(Path.GetFileName(dir)));
            demDropdown.AddOptions(demOptions);
        }

        // Cache flightline options
        if (Directory.Exists(flightlinesPath))
            cachedFlightlineOptions = new List<string>(Directory.GetDirectories(flightlinesPath));

        // Populate existing initial dropdown (first Flightline)
        PopulateDropdown(initialDropdown.GetComponentInChildren<TMP_Dropdown>());

        loadButton.interactable = true;
    }

    public void AddDropdown()
    {
        GameObject newDropdownObject = Instantiate(dropdownPrefab, dropdownContainer);
        Button deleteButton = newDropdownObject.GetComponentInChildren<Button>();
        deleteButton.onClick.AddListener(() => DeleteDropdown(newDropdownObject));

        RectTransform rect = newDropdownObject.GetComponent<RectTransform>();
        rect.anchoredPosition = GetPositonForIndex(dropdownList.Count);
        dropdownList.Add(newDropdownObject);

        TMP_Dropdown dropdown = newDropdownObject.GetComponentInChildren<TMP_Dropdown>();
        SetLoadingState(dropdown);
        PopulateDropdown(dropdown);
    }

    private void PopulateDropdown(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (string path in cachedFlightlineOptions)
            options.Add(new TMP_Dropdown.OptionData(Path.GetFileName(path)));
        dropdown.AddOptions(options);
    }

    private void SetLoadingState(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "Loading..." });
    }

    public void HandleLoad()
    {
        DataLoader.Instance.flightlineDirectories.Clear();

        // DEM selection
        TMP_Dropdown demDropdown = sceneDropdown.GetComponentInChildren<TMP_Dropdown>();
        if (demDropdown != null)
        {
            string selectedDem = demDropdown.options[demDropdown.value].text;
            
            #if UNITY_EDITOR
                string demPath = ("Assets/AppData/DEMs");
                string fullPath = Path.Combine(demPath, selectedDem);
            #else
                string fullPath = Path.Combine(Application.persistentDataPath, "Assets", "AppData", "DEMs", selectedDem);
            #endif
            
            DataLoader.Instance.demDirectoryPath = fullPath;
        }

        // Flightline selections
        foreach (var dropdownObj in dropdownList)
        {
            TMP_Dropdown dropdown = dropdownObj.GetComponentInChildren<TMP_Dropdown>();
            if (dropdown != null && Application.isEditor)
            {
                string selectedFlightline = dropdown.options[dropdown.value].text;
                string flightlinesPath = ("Assets/AppData/Flightlines");
                string fullPath = Path.Combine(flightlinesPath, selectedFlightline);
                DataLoader.Instance.flightlineDirectories.Add(fullPath);
            }
            else if (dropdown != null && !Application.isEditor)  // Application.Android and UNITY_ANDROID do not work as conditional IDs this recognizes
            {
                string selectedFlightline = dropdown.options[dropdown.value].text;
                string fullPath = Path.Combine(Application.persistentDataPath, "Assets", "AppData", "Flightlines", selectedFlightline);
                DataLoader.Instance.flightlineDirectories.Add(fullPath);
            }
        }

        Debug.Log("DEM: " + DataLoader.Instance.demDirectoryPath);
        Debug.Log("Flightlines: " + string.Join(", ", DataLoader.Instance.flightlineDirectories));
        
        if (DataLoader.Instance.sceneSelected)
        {
            DataLoader.Instance.DeleteAllChildren();
            DataLoader.Instance.LoadSceneData();
        }
        else
        {
            DataLoader.Instance.sceneSelected = true;
        }

        user.position = DataLoader.Instance.GetDEMCentroid();
        gameObject.SetActive(false);
    }

    public void DeleteDropdown(GameObject dropdownObj)
    {
        if (dropdownList.Count > 1)
        {
            int deletedIndex = dropdownList.IndexOf(dropdownObj);
            dropdownList.RemoveAt(deletedIndex);
            Destroy(dropdownObj);
            for (int i = deletedIndex; i < dropdownList.Count; i++)
            {
                RectTransform rect = dropdownList[i].GetComponent<RectTransform>();
                rect.anchoredPosition = GetPositonForIndex(i);
            }
        }
    }

    private Vector3 GetPositonForIndex(int index)
    {
        return new Vector3(startPosition.x, startPosition.y + index * verticalSpacing, startPosition.z);
    }

    public void GetSelectedDropdownTexts()
    {
        List<string> texts = new List<string>();
        foreach (var dropdownObj in dropdownList)
        {
            TMP_Dropdown dropdown = dropdownObj.GetComponentInChildren<TMP_Dropdown>();
            if (dropdown != null)
            {
                texts.Add(dropdown.options[dropdown.value].text);
            }
        }

        foreach (var text in texts)
        {
            Debug.Log(text);
        }
    }

    void Update()
    {
        Vector3 cameraForward = userCamera.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        Vector3 targetPosition = userCamera.position + cameraForward * distance;
        Vector3 directionToCamera = userCamera.position - transform.position;
        directionToCamera.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);

        float lerpSpeed = 7.5f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
    }
}
