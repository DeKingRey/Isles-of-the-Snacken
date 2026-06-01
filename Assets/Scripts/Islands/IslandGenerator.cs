using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.AI.Navigation;
using System.Collections;

public class IslandGenerator : NetworkBehaviour
{
    [SerializeField] GameObject[] islandPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] int islandCount = 10;
    [SerializeField] float spawnRadius = 200f;
    [SerializeField] LayerMask waterLayer;
    [SerializeField] float spacingPadding = 10f;

    private List<PlacedIsland> placedIslands = new List<PlacedIsland>();
    private List<int> prefabBag = new List<int>();

    private NavMeshSurface surface; 

    private class PlacedIsland
    {
        public Vector3 position;
        public float radius;

        public PlacedIsland(Vector3 pos, float r)
        {
            position = pos;
            radius = r;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        GenerateIslands();
    }

    // Delayed slightly to ensure that all objects are initialized
    private IEnumerator DelayBakeNavMesh()
    {
        yield return null;
        yield return null;

        surface = GetComponentInChildren<NavMeshSurface>();
        surface.BuildNavMesh();

        yield return null;

        foreach (var spawner in FindObjectsOfType<NommianSpawner>())
        {
            spawner.SpawnNommians();
        }
    }

    public void GenerateIslands()
    {
        if (islandPrefabs == null || islandPrefabs.Length == 0)
            return;
        
        // Resets island placements
        placedIslands.Clear();
        RefillBag();

        // Spawns the islands
        for (int i = 0; i < islandCount; i++)
        {
            GameObject island = GetNextPrefab();
            float islandRadius = island.GetComponent<IslandSize>().islandRadius;

            // Only spawns if far enough away from other islands
            if (TryGetValidPosition(islandRadius, out Vector3 spawnPos))
            {
                SpawnIsland(island, spawnPos, islandRadius);
            }
        }

        StartCoroutine(DelayBakeNavMesh());
    }

    bool TryGetValidPosition(float islandRadius, out Vector3 position)
    {
        int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(circle.x, 0f, circle.y);

            // Will only place islands on water
            if (waterLayer != 0)
            {
                Vector3 rayStart = candidate + Vector3.up * 100f;
                if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, waterLayer))
                    continue;
            }

            // Checks that island pos is far enough away from others
            if (IsFarEnough(candidate, islandRadius))
            {
                position = candidate;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    bool IsFarEnough(Vector3 candidate, float newRadius)
    {
        foreach (PlacedIsland island in placedIslands)
        {
            // Calculates min distance between islands and adds some randomness
            float minRequired = island.radius + newRadius + spacingPadding;
            minRequired *= Random.Range(0.9f, 1.15f);

            if (Vector3.Distance(candidate, island.position) < minRequired)
                return false;
        }

        return true;
    }

    void SpawnIsland(GameObject prefab, Vector3 position, float radius)
    {
        GameObject island = Instantiate(prefab, position, Quaternion.Euler(-90f, 0, 0));
        island.GetComponent<NetworkObject>().Spawn();

        island.transform.rotation = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f); // Random rotation
        placedIslands.Add(new PlacedIsland(position, radius));
    }

    void RefillBag()
    {
        prefabBag.Clear();
        for (int i = 0; i < islandPrefabs.Length; i++)
            prefabBag.Add(i);
    }

    GameObject GetNextPrefab()
    {
        // The bag makes it so it won't always be the same islands
        if (prefabBag.Count == 0)
            RefillBag();

        int bagIndex = Random.Range(0, prefabBag.Count);
        int prefabIndex = prefabBag[bagIndex];
        prefabBag.RemoveAt(bagIndex);

        return islandPrefabs[prefabIndex];
    }

    public void ClearIslands()
    {
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);

        placedIslands.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        if (placedIslands == null) return;

        Gizmos.color = Color.yellow;
        foreach (PlacedIsland island in placedIslands)
            Gizmos.DrawWireSphere(island.position, island.radius);
    }
}