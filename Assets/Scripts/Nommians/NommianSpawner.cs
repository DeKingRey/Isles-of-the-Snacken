using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class NommianSpawner : NetworkBehaviour
{
    private List<NommianController> nommians = new List<NommianController>();
    private List<NetworkObject> spawnedNommians = new List<NetworkObject>();
    private List<Transform> spawnpoints = new List<Transform>();
    private int spawnpointIndex = 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Gets all spawnpoints
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.CompareTag("Spawnpoint"))
            {
                spawnpoints.Add(child);
            }
        }
    }

    public void SpawnNommians()
    {
        // Random amount +-1 of the amount of spawnpoints
        int nommianAmount = Random.Range(spawnpoints.Count - 1, spawnpoints.Count + 1);

        // Gets a list of all nommians
        List<GameObject> availableNommians = new List<GameObject>();
        foreach (NommianDatabase.Nommian nommian in GameManager.Instance.nommianDatabase.nommians)    
        {
            availableNommians.Add(nommian.nommianPrefab);
        }

        for (int i = 0; i < nommianAmount; i++)
        {
            if (availableNommians.Count <= 0) break;

            GameObject nommianPrefab = availableNommians[Random.Range(0, availableNommians.Count)]; // Gets a random nommian
            
            // Instantiates nommian at current spawnpoint
            Transform spawnpoint = spawnpoints[spawnpointIndex];
            spawnpointIndex++;
            GameObject spawnedNommian = Instantiate(nommianPrefab, spawnpoint.position, Quaternion.identity);
            spawnedNommian.GetComponent<NetworkObject>().Spawn();

            // Disables all nommians to begin with
            var controller = spawnedNommian.GetComponent<NommianController>();
            controller.ToggleNommian(false);
            nommians.Add(controller);
            spawnedNommians.Add(spawnedNommian.GetComponent<NetworkObject>());
        }
    }

    public void DespawnNommians()
    {
        foreach (var nommian in spawnedNommians)
        {
            if (nommian != null && nommian.IsSpawned)
            {
                nommian.Despawn(true);
            }
        }

        nommians.Clear();
        spawnedNommians.Clear();
    }
}
