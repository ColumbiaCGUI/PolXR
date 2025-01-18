using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

public class MainMenu : MonoBehaviour
{
    public void ToHomeScene()
    {
        SceneManager.LoadScene("Home");
    }

    public void ToggleSurfaceDEM(bool arg0)
    {
        GameObject surfaceDEM = GameObject.Find("/Managers/DataLoader/DEM/surface");
        surfaceDEM.SetActive(!surfaceDEM.activeSelf);
    }

    public void ToggleBaseDEM(bool arg0)
    {
        GameObject baseDEM = GameObject.Find("/Managers/DataLoader/DEM/bedrock");
        baseDEM.SetActive(!baseDEM.activeSelf);
    }

    public void ToggleFlightlines()
    {
        GameObject radarRoot = GameObject.Find("/Managers/DataLoader/Radar");
        foreach (Transform radarChild in radarRoot.transform)
        {
            Transform flightline = radarChild.Find("Flightline");
            if (flightline != null)
            {
                flightline.gameObject.SetActive(!flightline.gameObject.activeSelf);
            }
        }
    }

    public void ResetScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    public void SaveScene()
    {
        //TO DO: not sure what should be done here lol 
    }

    public void Toggle(GameObject toggleBackground)
    {
        toggleBackground.SetActive(!toggleBackground.activeSelf);
    }

}