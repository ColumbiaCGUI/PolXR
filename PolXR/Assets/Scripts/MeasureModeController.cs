using UnityEngine;

public class MeasureModeController : MonoBehaviour
{
    public static bool IsMeasureModeActive { get; private set; } = false;
    public KeyCode toggleKey = KeyCode.M; // Press 'M' to toggle measure mode

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMeasureMode();
        }
    }

    public void ToggleMeasureMode()
    {
        IsMeasureModeActive = !IsMeasureModeActive;
        Debug.Log("Measure Mode: " + (IsMeasureModeActive ? "ON" : "OFF"));
    }

    public void SetMeasureMode(bool state)
    {
        IsMeasureModeActive = state;
        Debug.Log("Measure Mode Set: " + state);
    }
}
