using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StoneGeneration : MonoBehaviour
{
    public TerrainGenerator TerrainGenerator {  get; private set; }
    [SerializeField] private GameObject[] stonePrefabs;
    [SerializeField] private int maxStones = 100;
    [SerializeField] private Vector3 spawnAreaCenter;
    [SerializeField] private Vector3 spawnAreaSize;
    [SerializeField] private float sampleRadius = 2f;
    [SerializeField] private LayerMask terrainMask; // add your terrain layer here!
    [SerializeField] private LayerMask treeMask;
    [SerializeField] private Transform stoneParent;

    private int stoneCount = 0;

    public GeneratedType GenerationType = GeneratedType.None;

    public void SetGenerator(TerrainGenerator generator)
    {
        TerrainGenerator = generator;
    }

    public void GenerateStones()
    {
        Debug.Log("generating stones...");
        stoneCount = 0;
        int attempts = 0;
        int maxAttempts = maxStones * 40;
        // int failSafe = 100000; // prevent infinite loops

        while (stoneCount < maxStones && attempts < maxAttempts)
        {
            if (GetRandomPointOnNavMesh(spawnAreaCenter, spawnAreaSize, out Vector3 spawnPos))
            {
                bool canSpawn = false;
                float heat = 0f;
                float moisture = 0f;

                // Raycast down to terrain to get UV
                if (Physics.Raycast(spawnPos + Vector3.up * 500f, Vector3.down, out RaycastHit hit, 1000f, terrainMask))
                {
                    MeshCollider meshCol = hit.collider as MeshCollider;
                    if (meshCol != null && meshCol.sharedMesh != null)
                    {
                        Mesh mesh = meshCol.sharedMesh;
                        int triIndex = hit.triangleIndex * 3;
                        if (triIndex + 2 < mesh.triangles.Length)
                        {
                            int[] tris = mesh.triangles;
                            Vector3[] verts = mesh.vertices;
                            Vector2[] uvs = mesh.uv;
                            Vector2[] uv1s = mesh.uv2;
                            Vector2[] uv2s = mesh.uv3;
                            Vector2[] uv3s = mesh.uv4;

                            Vector2 uv2Value = Vector2.zero;
                            if (uv2s != null && uv2s.Length == verts.Length)
                            {
                                Vector2 uv20 = uv2s[tris[triIndex]];
                                Vector2 uv21 = uv2s[tris[triIndex + 1]];
                                Vector2 uv22 = uv2s[tris[triIndex + 2]];
                                uv2Value = uv20 * hit.barycentricCoordinate.x +
                                        uv21 * hit.barycentricCoordinate.y +
                                        uv22 * hit.barycentricCoordinate.z;

                                // Debug.Log($"Trying to spawn resource at world {hit.point} | UV {uv2Value}");

                                // --- Biome-based filtering logic ---
                                heat = uv2Value.x;
                                moisture = uv2Value.y;

                                GameObject samplePrefab = stonePrefabs[Random.Range(0, stonePrefabs.Length)];
                                string name = samplePrefab.name.ToLower();

                                // Example biome filtering
                                if (name.Contains("stone"))
                                {
                                    GenerationType = GeneratedType.Stone;
                                    // Stones: spawn mostly in hot places
                                    if (heat < 0.1f) //&& moisture < 0.7f)
                                        canSpawn = true;
                                }
                                else if (name.Contains("wheat"))
                                {
                                    GenerationType = GeneratedType.Wheat;
                                    // Wheat: likes moderate heat and higher moisture
                                    if (heat > 0.3f && moisture > 0.35f)
                                        canSpawn = true;
                                }
                                else if (name.Contains("copperore"))
                                {
                                    GenerationType = GeneratedType.Copper;
                                    // Copper ore: prefers hotter, drier areas
                                    if (heat < 0.2f && moisture < 0.4f)
                                        canSpawn = true;
                                }
                                else if (name.Contains("tinore"))
                                {
                                    GenerationType = GeneratedType.Tin;
                                    // Tin ore: prefers cooler, damp regions
                                    if (heat > 0.5f)
                                        canSpawn = true;
                                }
                            }
                        }
                    }
                    else { Debug.Log("É DEU RUIM KKKKKKKKK"); }
                }
                else { Debug.Log("Não colidiu com o terreno"); }

                // Instantiate stone only if biome allows
                if (canSpawn)
                {
                    GameObject prefab = stonePrefabs[Random.Range(0, stonePrefabs.Length)];
                    GameObject stone = Instantiate(prefab, spawnPos, Quaternion.identity);

                    //stone.transform.SetParent(this.transform, true);

                    switch (GenerationType)
                    {
                        case GeneratedType.Stone:
                            if(!TerrainGenerator.GeneratedStone.Contains(stone))
                                TerrainGenerator.GeneratedStone.Add(stone);
                            break;
                        case GeneratedType.Copper:
                            if (!TerrainGenerator.GeneratedCopper.Contains(stone))
                                TerrainGenerator.GeneratedCopper.Add(stone);
                            break;
                        case GeneratedType.Tin:
                            if (!TerrainGenerator.GeneratedTin.Contains(stone))
                                TerrainGenerator.GeneratedTin.Add(stone);
                            break;
                        case GeneratedType.Wheat:
                            if (!TerrainGenerator.GeneratedWheat.Contains(stone))
                                TerrainGenerator.GeneratedWheat.Add(stone);
                            break;
                    }

                    float randomScale = Random.Range(0.2f, 1f);
                    if (prefab.name != "wheat")
                        stone.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                    stone.transform.Rotate(0, Random.Range(0f, 360f), 0f, Space.Self);

                    //if (stoneParent != null)
                        //stone.transform.parent = stoneParent;

                    stoneCount++;
                }
            }
            attempts++;
        }

        Debug.Log($"Stones generated: {stoneCount}");
    }

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
public enum GeneratedType
{
    None,
    Tree,
    Stone,
    Tin,
    Copper,
    Wheat
}