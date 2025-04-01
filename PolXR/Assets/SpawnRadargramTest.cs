using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SpawnRadargramTest : MonoBehaviour
{
    [SerializeField] private NetworkRunner runner;
    private BakingObjectProvider provider;

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

        // Get and validate the provider
        provider = runner.GetComponent<BakingObjectProvider>();
        if (provider == null)
        {
            Debug.LogError("BakingObjectProvider not found on NetworkRunner!");
            yield break;
        }

        // Check if Runner is valid
        if (runner.IsRunning)
        {
            Debug.Log($"NetworkRunner connected and ready! State: {runner.State}");

            try
            {
                provider.ValidateRunnerState(runner);
                SpawnRadargram(1);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error during spawn attempt: {ex.Message}\n{ex.StackTrace}");
            }
        }
        else
        {
            Debug.LogError($"NetworkRunner is not properly initialized! State: {runner.State}");
        }
    }

    public void SpawnRadargram(int segmentIndex)
    {
        if (runner == null || !runner.IsRunning)
        {
            Debug.LogError($"Cannot spawn - NetworkRunner invalid! IsNull: {runner == null}, IsRunning: {runner?.IsRunning}");
            return;
        }

        if (provider == null)
        {
            Debug.LogError("BakingObjectProvider not found!");
            return;
        }

        Debug.Log($"Attempting to spawn radargram with segment index {segmentIndex}...");
        Debug.Log($"Runner State: {runner.State}, IsSceneAuthority: {runner.IsSceneAuthority}");

        try
        {
            uint customPrefabId = (uint)(BakingObjectProvider.CUSTOM_PREFAB_FLAG + segmentIndex);
            NetworkPrefabId prefabId = new NetworkPrefabId() { RawValue = customPrefabId };

            // In shared mode, we use state authority for the spawning peer
            var spawnedObj = runner.Spawn(
                prefabId,
                position: Vector3.zero,
                rotation: Quaternion.identity,
                onBeforeSpawned: (runner, obj) =>
                {
                    Debug.Log($"Spawning shared object: {obj.name}");
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
                Debug.Log($"Successfully spawned shared radargram! NetworkObject valid: {spawnedObj.IsValid}, ID: {spawnedObj.Id}, HasStateAuthority: {spawnedObj.HasStateAuthority}");
            }
            else
            {
                Debug.LogError("Spawn returned null object!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during spawn: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
