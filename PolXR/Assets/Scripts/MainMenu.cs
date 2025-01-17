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

}
