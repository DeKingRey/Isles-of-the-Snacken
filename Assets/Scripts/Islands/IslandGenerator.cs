using UnityEngine;
using System.Collections.Generic;

public class IslandGenerator : MonoBehaviour
{
    [Header("Island Prefabs")]
    [SerializeField] GameObject[] islandPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] int islandCount = 10;
    [SerializeField] float spawnRadius = 200f;
    [SerializeField] LayerMask waterLayer;

    [Header("Spacing")]
    [SerializeField] float minDistanceBetweenIslands = 50f;
    [SerializeField] float spacingPadding = 10f;

    [Header("Auto Generate")]
    [SerializeField] bool generateOnStart = false;
    [SerializeField] bool useRandomSeed = true;
    [SerializeField] int seed = 0;

    private List<PlacedIsland> placedIslands = new List<PlacedIsland>();
    private List<int> prefabBag = new List<int>();

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

    void Start()
    {
        if (generateOnStart)
            GenerateIslands();
    }

    public void GenerateIslands()
    {
        if (islandPrefabs == null || islandPrefabs.Length == 0)
        {
            return;
        }

        if (useRandomSeed)
        {
            seed = Random.Range(0, 999999);
        }

        Random.InitState(seed);
        placedIslands.Clear();
        RefillBag();

        for (int i = 0; i < islandCount; i++)
        {
            GameObject prefab = GetNextPrefab();
            float islandRadius = GetIslandRadius(prefab);

            if (TryGetValidPosition(islandRadius, out Vector3 spawnPos))
            {
                SpawnIsland(prefab, spawnPos, islandRadius);
            }
        }
    }

    bool TryGetValidPosition(float islandRadius, out Vector3 position)
    {
        int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = transform.position + new Vector3(circle.x, 0f, circle.y);

            if (waterLayer != 0)
            {
                Vector3 rayStart = candidate + Vector3.up * 100f;
                if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, waterLayer))
                    continue;
            }

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
        foreach (PlacedIsland other in placedIslands)
        {
            float minRequired = other.radius + newRadius + spacingPadding;
            minRequired *= Random.Range(0.9f, 1.15f);

            if (Vector3.Distance(candidate, other.position) < minRequired)
                return false;
        }

        return true;
    }

    void SpawnIsland(GameObject prefab, Vector3 position, float radius)
    {
        GameObject island = Instantiate(prefab, position, Quaternion.identity, transform);
        island.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        placedIslands.Add(new PlacedIsland(position, radius));
    }

    float GetIslandRadius(GameObject prefab)
    {
        IslandSize sizeComp = prefab.GetComponent<IslandSize>();
        if (sizeComp != null)
            return sizeComp.GetScaledRadius();

        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);

            float estimated = Mathf.Max(bounds.extents.x, bounds.extents.z);
            return estimated;
        }
        
        return 30f;
    }

    void RefillBag()
    {
        prefabBag.Clear();
        for (int i = 0; i < islandPrefabs.Length; i++)
            prefabBag.Add(i);
    }

    GameObject GetNextPrefab()
    {
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