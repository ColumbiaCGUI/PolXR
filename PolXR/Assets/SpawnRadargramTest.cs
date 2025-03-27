using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SpawnRadargramTest : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner runner;
    
    // Start is called before the first frame update
    void Start()
    {
        if (runner != null)
        {
            // Check if the NetworkRunner is using BakingObjectProvider
            var existingProvider = runner.GetComponent<INetworkObjectProvider>();
            
            if (existingProvider == null || !(existingProvider is BakingObjectProvider))
            {
                Debug.LogWarning("NetworkRunner is not using BakingObjectProvider! Adding one now.");
                
                // Remove any existing provider
                if (existingProvider != null && existingProvider is Component)
                {
                    Destroy((Component)existingProvider);
                }
                
                // Add our custom provider
                runner.gameObject.AddComponent<BakingObjectProvider>();
            }
            else
            {
                Debug.Log("NetworkRunner is already using BakingObjectProvider");
            }
        }
        
        // Use coroutine instead of direct call
        StartCoroutine(DelayedSpawn());

        // Setup event listener
        if (runner != null)
        {
            runner.AddCallbacks(this);
        }
    }

    IEnumerator DelayedSpawn()
    {
        Debug.Log("Starting delayed spawn");

        while(runner.State != NetworkRunner.States.Running)
        {
            Debug.Log("Waiting for runner to be running, current state: " + runner.State);
            yield return new WaitForSeconds(0.2f);
        }
        
        // Add crucial diagnostic checks
        if (runner == null) {
            Debug.LogError("Runner is null!");
            yield break;
        }
        
        if (runner.State != NetworkRunner.States.Running) {
            Debug.LogError($"Cannot spawn - NetworkRunner not in Running state! Current state: {runner.State}");
            yield break;
        }
        
        // Get and validate the provider
        var provider = runner.GetComponent<BakingObjectProvider>();
        if (provider != null) {
            provider.ValidateRunnerState(runner);
        } else {
            Debug.LogError("BakingObjectProvider is missing from runner!");
            yield break;
        }
        
        // Only spawn if everything looks good
        Debug.Log("Starting delayed spawn");
        SpawnRadargram(1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnRadargram(int segmentIndex)
    {
        if (runner == null)
        {
            Debug.LogError("NetworkRunner reference is missing!");
            return;
        }

        // Convert segment index to a custom prefab ID
        // The BakingObjectProvider uses (prefabId - 100000) as the segment folder index
        uint customPrefabId = (uint)(BakingObjectProvider.CUSTOM_PREFAB_FLAG + segmentIndex);
        
        // Use NetworkPrefabId with the raw value
        NetworkPrefabId prefabId = new NetworkPrefabId() { RawValue = customPrefabId };
        
        // Spawn the radargram
        runner.Spawn(prefabId, position: Vector3.zero, rotation: Quaternion.identity);
    }

    // Implement INetworkRunnerCallbacks
    public void OnRunnerConnectionStatusChanged(NetworkRunner runner, ConnectionStatus status)
    {
        Debug.Log($"Runner status changed: {status}");
        
        if (status == ConnectionStatus.Connected)
        {
            Debug.Log("NetworkRunner connected and ready to spawn!");
            SpawnRadargram(1);
        }
    }
}
