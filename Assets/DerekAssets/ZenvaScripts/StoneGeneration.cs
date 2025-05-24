using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StoneGeneration : MonoBehaviour
{
    [SerializeField] private GameObject[] stonePrefabs;
    [SerializeField] private int maxStones = 100;
    [SerializeField] private Vector3 spawnAreaCenter;
    [SerializeField] private Vector3 spawnAreaSize;
    [SerializeField] private float sampleRadius = 2f;
    [SerializeField] private LayerMask treeMask;

    [SerializeField] private Transform stoneParent;

    private int stoneCount = 0;

    public void GenerateStones()
    {
        Debug.Log("generating stones...");
        stoneCount = 0;
        int attempts = 0;
        int maxAttempts = maxStones * 10;

        while (stoneCount < maxStones && attempts < maxAttempts)
        {
            if (GetRandomPointOnNavMesh(spawnAreaCenter, spawnAreaSize, out Vector3 spawnPos))
            {
                GameObject prefab = stonePrefabs[Random.Range(0, stonePrefabs.Length)];
                GameObject stone = Instantiate(prefab, spawnPos, Quaternion.identity);

                stone.transform.SetParent(this.transform);

                if (stoneParent != null)
                    stone.transform.parent = stoneParent;

                stoneCount++;
            }
            attempts++;
        }

        Debug.Log($"Stones generated: {stoneCount}");
    }

    // This method is now inside StoneGeneration
    private bool GetRandomPointOnNavMesh(Vector3 center, Vector3 size, out Vector3 result, int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPoint = center + new Vector3(
                Random.Range(-size.x / 2f, size.x / 2f),
                Random.Range(-size.y / 2f, size.y / 2f),
                Random.Range(-size.z / 2f, size.z / 2f)
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(randomPoint, hit.position) < 1.0f)
                {
                    result = hit.position;
                    return true;
                }
            }
        }

        result = Vector3.zero;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }

}
