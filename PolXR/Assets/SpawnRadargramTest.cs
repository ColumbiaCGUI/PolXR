using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SpawnRadargramTest : MonoBehaviour
{
    [SerializeField] private NetworkRunner runner;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitForRunner());
    }

    IEnumerator WaitForRunner()
    {
        // Initial delay to let everything initialize
        yield return new WaitForSeconds(1.0f);

        // Wait for runner to be initialized and connected
        float timeout = 15f;
        float elapsed = 0f;

        while ((runner == null || runner.State != NetworkRunner.States.Running || !runner.IsRunning) && elapsed < timeout)
        {
            Debug.Log($"Waiting for NetworkRunner... Current state: {runner?.State}, IsRunning: {runner?.IsRunning}");
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (runner == null || runner.State != NetworkRunner.States.Running || !runner.IsRunning)
        {
            Debug.LogError("NetworkRunner failed to reach running state within timeout!");
            yield break;
        }

        // Additional check to verify the Runner is valid
        if (runner.IsRunning)
        {
            Debug.Log("NetworkRunner connected and ready to spawn!");

            // Get the provider type
            var provider = runner.GetComponent<INetworkObjectProvider>();
            Debug.Log($"Using provider: {provider?.GetType().Name}");

            // Try simple object first
            try
            {
                Debug.Log("Attempting to spawn a simple test object first...");

                // Use a special prefab ID for a test object
                // 99999 will be handled by the special case in BakingObjectProvider
                NetworkPrefabId testPrefabId = new NetworkPrefabId() { RawValue = 99999 };
                var testObj = runner.Spawn(testPrefabId, position: Vector3.zero, rotation: Quaternion.identity);

                Debug.Log($"Spawned test object successfully: {testObj != null}");

                // If test object works, try the radargram
                SpawnRadargram(1);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error spawning test object: {ex.Message}\n{ex.StackTrace}");
            }
        }
        else
        {
            Debug.LogError($"NetworkRunner is not running properly! IsRunning: {runner.IsRunning}");
        }
    }

    public void SpawnRadargram(int segmentIndex)
    {
        if (runner == null)
        {
            Debug.LogError("NetworkRunner reference is missing!");
            return;
        }

        // Safety check
        if (!runner.IsRunning)
        {
            Debug.LogError("Cannot spawn - NetworkRunner is not running!");
            return;
        }

        Debug.Log($"Attempting to spawn radargram with segment index {segmentIndex}...");

        // Convert segment index to a custom prefab ID
        // The BakingObjectProvider uses (prefabId - 100000) as the segment folder index
        uint customPrefabId = (uint)(BakingObjectProvider.CUSTOM_PREFAB_FLAG + segmentIndex);

        // Use NetworkPrefabId with the raw value
        NetworkPrefabId prefabId = new NetworkPrefabId() { RawValue = customPrefabId };

        // Spawn the radargram
        runner.Spawn(prefabId, position: Vector3.zero, rotation: Quaternion.identity);
    }
}
