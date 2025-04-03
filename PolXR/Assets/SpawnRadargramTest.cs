using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SpawnRadargramTest : MonoBehaviour
{
    [SerializeField] private NetworkRunner runner;
    // private BakingObjectProvider provider;

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
            Debug.Log($"[SpawnRadargramTest [WaitForRunner]] Waiting for NetworkRunner... Current state: {runner?.State}, IsRunning: {runner?.IsRunning}");
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }

        if (runner == null || runner.State != NetworkRunner.States.Running || !runner.IsRunning)
        {
            Debug.LogError($"[SpawnRadargramTest [WaitForRunner]] NetworkRunner failed to reach running state within timeout!");
            yield break;
        }

        // Get and validate the provider
        // provider = runner.GetComponent<BakingObjectProvider>();
        // if (provider == null)
        // {
        //     Debug.LogError($"[SpawnRadargramTest [WaitForRunner]] BakingObjectProvider not found on NetworkRunner!");
        //     yield break;
        // }

        // Check if Runner is valid
        if (runner.IsRunning)
        {
            Debug.Log($"[SpawnRadargramTest [WaitForRunner]] NetworkRunner connected and ready! State: {runner.State}");

            try
            {
                // provider.ValidateRunnerState(runner);
                SpawnRadargram(1);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SpawnRadargramTest [WaitForRunner]] Error during spawn attempt: {ex.Message}\n{ex.StackTrace}");
            }
        }
        else
        {
            Debug.LogError($"[SpawnRadargramTest [WaitForRunner]] NetworkRunner is not properly initialized! State: {runner.State}");
        }
    }

    public void SpawnRadargram(int segmentIndex)
    {
        // if (runner == null || !runner.IsRunning)
        // {
        //     Debug.LogError($"[SpawnRadargramTest [SpawnRadargram]] Cannot spawn - NetworkRunner invalid! IsNull: {runner == null}, IsRunning: {runner?.IsRunning}");
        //     return;
        // }

        // if (provider == null)
        // {
        //     Debug.LogError($"[SpawnRadargramTest [SpawnRadargram]] BakingObjectProvider not found!");
        //     return;
        // }

        Debug.Log($"[SpawnRadargramTest [SpawnRadargram]] Attempting to spawn radargram with segment index {segmentIndex}...");
        Debug.Log($"[SpawnRadargramTest [SpawnRadargram]] Runner State: {runner.State}, IsSceneAuthority: {runner.IsSceneAuthority}");

        try
        {
            // Add additional validation for runner's state
            if (runner.State != NetworkRunner.States.Running)
            {
                Debug.LogError($"[SpawnRadargramTest [SpawnRadargram]] Runner not in Running state. Current state: {runner.State}");
                StartCoroutine(RetrySpawn(segmentIndex));
                return;
            }

            uint customPrefabId = (uint)(BakingObjectProvider.CUSTOM_PREFAB_FLAG + segmentIndex);
            NetworkPrefabId prefabId = new NetworkPrefabId() { RawValue = customPrefabId };

            // Check if we already have a spawned object with this ID
            var existingName = $"Our Radargram_{customPrefabId}";
            var existingObj = GameObject.Find(existingName);
            if (existingObj != null)
            {
                Debug.LogWarning($"[SpawnRadargramTest [SpawnRadargram]] Object {existingName} already exists in scene. Skipping spawn.");
                return;
            }

            // In shared mode, we use state authority for the spawning peer
            var spawnedObj = runner.Spawn(
                prefabId,
                position: Vector3.zero,
                rotation: Quaternion.identity,
                onBeforeSpawned: (runner, obj) =>
                {
                    Debug.Log($"[SpawnRadargramTest [SpawnRadargram]] Spawning shared object: {obj.name}");
                    var no = obj.GetComponent<NetworkObject>();
                    if (no != null)
                    {
                        // Ensure we have state authority for initial setup
                        no.RequestStateAuthority();
                    }
                }
            );

            if (spawnedObj != null)
            {
                Debug.Log($"[SpawnRadargramTest [SpawnRadargram]] Successfully spawned shared radargram! NetworkObject valid: {spawnedObj.IsValid}, ID: {spawnedObj.Id}, HasStateAuthority: {spawnedObj.HasStateAuthority}");
            }
            else
            {
                Debug.LogError($"[SpawnRadargramTest [SpawnRadargram]] Spawn returned null object!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpawnRadargramTest [SpawnRadargram]] Error during spawn: {ex.Message}\n{ex.StackTrace}");
            // If we get a null reference, retry after a delay
            if (ex is System.NullReferenceException)
            {
                StartCoroutine(RetrySpawn(segmentIndex));
            }
        }
    }

    private IEnumerator RetrySpawn(int segmentIndex)
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"[SpawnRadargramTest [RetrySpawn]] Retrying spawn...");
        SpawnRadargram(segmentIndex);
    }
}
