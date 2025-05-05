using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HomeMenuController : MonoBehaviour
{
    public Transform user;
    private float distance=0.5f;
    public GameObject dropdownPrefab;
    public Transform dropdownContainer;
    public Button addButton;
    public RectTransform initialDropdown;
    private Vector3 startPosition;
    private List<GameObject> dropdownList= new List<GameObject>();
    private float verticalSpacing= -9f; 

    // Start is called before the first frame update
    void Start()
    {
        startPosition= initialDropdown.anchoredPosition;
        dropdownList.Add(initialDropdown.gameObject);
        Button initialDltBtn=initialDropdown.GetComponentInChildren<Button>();
        initialDltBtn.onClick.AddListener(()=>DeleteDropdown(initialDropdown.gameObject));
        addButton.onClick.AddListener(AddDropdown);
    }

    public void AddDropdown(){
        //create new dropdown
        GameObject newDropdownObject= Instantiate(dropdownPrefab, dropdownContainer);
        Button deleteButton= newDropdownObject.GetComponentInChildren<Button>();
        deleteButton.onClick.AddListener(()=>DeleteDropdown(newDropdownObject));
        RectTransform rect= newDropdownObject.GetComponent<RectTransform>();
        rect.anchoredPosition= GetPositonForIndex(dropdownList.Count);
        //add the new dropdown to list & set position
        dropdownList. Add(newDropdownObject);
       
    }
    
    public void DeleteDropdown(GameObject dropdownObj){
        if(dropdownList.Count>1){
            int deletedIndex= dropdownList.IndexOf(dropdownObj);
            dropdownList.RemoveAt(deletedIndex);
            Destroy(dropdownObj);
            for(int i=deletedIndex;i<dropdownList.Count;i++){
                RectTransform rect = dropdownList[i].GetComponent<RectTransform>();
                rect.anchoredPosition=GetPositonForIndex(i);
            }
        }
    }

    private Vector3 GetPositonForIndex(int index){
        return new Vector3(startPosition.x, startPosition.y+ index*verticalSpacing,startPosition.z);
    }

    public void GetSelectedDropdownTexts(){
        List<string> texts= new List<string>();
        foreach (var dropdownObj in dropdownList){
            TMP_Dropdown dropdown= dropdownObj.GetComponentInChildren<TMP_Dropdown>();
            if(dropdown!=null){
                texts.Add(dropdown.options[dropdown.value].text);
            }

        }
        foreach(var text in texts){
            Debug.Log(text);
        }
        
    }

    // Update is called once per frame
void Update()
{
    Vector3 cameraForward = user.forward;
    cameraForward.y = 0;
    cameraForward.Normalize();
    Vector3 targetPosition = user.position + cameraForward * distance;
    Vector3 directionToCamera = user.position - transform.position;
    directionToCamera.y = 0;
    Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);

    // Smooth follow with Lerp/Slerp
    float lerpSpeed = 7.5f; 
    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
}
}
