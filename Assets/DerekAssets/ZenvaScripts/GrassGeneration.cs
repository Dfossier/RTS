using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class GrassGeneration : MonoBehaviour
{
    public GameObject grassPrefab;
    public int grassCount = 1000;
    public Vector3 areaSize = new Vector3(50, 0, 50);

    void Start()
    {
        // GenerateGrassOnNavMesh();
    }

    public void GenerateGrassOnNavMesh()
    {
        MeshFilter grassFilter = grassPrefab.GetComponent<MeshFilter>();
        MeshRenderer grassRenderer = grassPrefab.GetComponent<MeshRenderer>();

        if (grassFilter == null || grassRenderer == null)
        {
            Debug.LogError("Grass prefab must have a MeshFilter and MeshRenderer.");
            return;
        }

        List<CombineInstance> combines = new List<CombineInstance>();
        Material grassMaterial = grassRenderer.sharedMaterial;

        int placed = 0;
        int attempts = 0;
        int maxAttempts = grassCount * 10;

        while (placed < grassCount && attempts < maxAttempts)
        {
            attempts++;

            Vector3 randomPos = new Vector3(
                Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
                0,
                Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
            ) + transform.position;

            if (NavMesh.SamplePosition(randomPos, out NavMeshHit navHit, 6f, NavMesh.AllAreas))
            {
                Vector3 navPos = navHit.position;

                Matrix4x4 matrix = Matrix4x4.TRS(
                    navPos,
                    Quaternion.Euler(0, Random.Range(0f, 360f), 0),
                    Vector3.one * Random.Range(2, 4) // Random.Range(0.8f, 1.2f)
                );

                CombineInstance combine = new CombineInstance
                {
                    mesh = grassFilter.sharedMesh,
                    transform = matrix
                };

                combines.Add(combine);
                placed++;
            }
        }

        Mesh combinedMesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        combinedMesh.CombineMeshes(combines.ToArray(), true);

        GameObject grassObject = new GameObject("CombinedGrass", typeof(MeshFilter), typeof(MeshRenderer));
        grassObject.GetComponent<MeshFilter>().sharedMesh = combinedMesh;
        grassObject.GetComponent<MeshRenderer>().material = grassMaterial;
        grassObject.transform.SetParent(transform);
    }
}
