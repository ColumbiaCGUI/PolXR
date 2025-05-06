using UnityEngine;
using UnityEngine.InputSystem;

public class HomeMenuToggle : MonoBehaviour
{
    public InputActionReference toggleHomeMenuButton;
    public GameObject homeMenu;

    void OnEnable()
    {
        toggleHomeMenuButton.action.performed += ToggleMenu;
        toggleHomeMenuButton.action.Enable();
    }

    void OnDisable()
    {
        toggleHomeMenuButton.action.performed -= ToggleMenu;
        toggleHomeMenuButton.action.Disable();
    }

    private void ToggleMenu(InputAction.CallbackContext context)
    {
        if (homeMenu != null)
            homeMenu.SetActive(!homeMenu.activeSelf);
    }
}
