using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TreeGeneration : MonoBehaviour
{
    //[SerializeField]
    //private Object trackingSphere;

    [SerializeField] public GameObject[] treeList;
    public int treeCount = 0;

    [SerializeField] private int maxTrees;
    [SerializeField] private int minorTreeRadius;
    [SerializeField] private float denseMoisture;

    [SerializeField] private GameObject[] treePrefab;
    [SerializeField] private float waterHeight = 2f;
    [SerializeField] private float treeHeat = .2f;

    [SerializeField] private float minTreeSpacing = .4f; // Min distance between trees

    private int biomeIndex = 1;
    private float neighborRadius;
    private List<Vector3> placedTreePositions = new List<Vector3>(); // Track tree positions

    public void GenerateTrees(int tilesWidth, int verticesWidth, TerrainData terrainData)
    {
        HeightMap[,] heightMaps = new HeightMap[tilesWidth, tilesWidth];

        for (int x = 0; x < tilesWidth; x++)
        {
            for (int z = 0; z < tilesWidth; z++)
            {
                heightMaps[x, z] = terrainData.chunksData[x, z];
            }
        }

        for (int xIndex = 0; xIndex < tilesWidth * (verticesWidth - 2); xIndex++)
        {
            for (int zIndex = 1; zIndex < tilesWidth * (verticesWidth - 2); zIndex++)
            {
                if (treeCount > maxTrees) break;

                TileCoordinate tileCoordinate = terrainData.ConvertToTileCoordinate(xIndex, zIndex);
                HeightMap treeHeightMap = heightMaps[tileCoordinate.tileXIndex, tileCoordinate.tileZIndex];

                neighborRadius = minorTreeRadius;
                float maxValue = 0f;

                float treeValue = treeHeightMap.moisture[tileCoordinate.coordinateXIndex, tileCoordinate.coordinateZIndex];
                if (treeValue >= denseMoisture)
                    neighborRadius = 0;

                int neighborXBegin = Mathf.Max(0, xIndex - (int)neighborRadius);
                int neighborXEnd = Mathf.Min(tilesWidth * verticesWidth - 2 * tilesWidth, xIndex + (int)neighborRadius);
                int neighborZBegin = Mathf.Max(0, zIndex - (int)neighborRadius);
                int neighborZEnd = Mathf.Min(tilesWidth * verticesWidth - 2 * tilesWidth, zIndex + (int)neighborRadius);

                for (int neighborZ = neighborZBegin; neighborZ <= neighborZEnd; neighborZ++)
                {
                    for (int neighborX = neighborXBegin; neighborX <= neighborXEnd; neighborX++)
                    {
                        TileCoordinate neighborCoordinate = terrainData.ConvertToTileCoordinate(neighborX, neighborZ);
                        HeightMap neighborHeightMap = terrainData.chunksData[neighborCoordinate.tileXIndex, neighborCoordinate.tileZIndex];

                        float neighborValue = neighborHeightMap.moisture[neighborCoordinate.coordinateXIndex, neighborCoordinate.coordinateZIndex];
                        if (neighborValue - 0.05f >= maxValue)
                        {
                            maxValue = neighborValue;
                        }
                    }
                }

                if (treeValue >= maxValue)
                {
                    float height = treeHeightMap.heightvalues[tileCoordinate.coordinateXIndex, tileCoordinate.coordinateZIndex];
                    float heat = treeHeightMap.heat[tileCoordinate.coordinateXIndex, tileCoordinate.coordinateZIndex];

                    if (height >= waterHeight && heat <= treeHeat)
                    {
                        // Original intended tree position
                        Vector3 originalPosition = new Vector3(
                            xIndex - 1 - (tileCoordinate.tileXIndex * 3),
                            height,
                            (tileCoordinate.tileZIndex + 1) * (verticesWidth - 3) - (tileCoordinate.coordinateZIndex - 4)
                        );

                        // Snap to NavMesh (keep Y = height)
                        Vector3 finalPosition = originalPosition;
                        if (NavMesh.SamplePosition(originalPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                        {
                            finalPosition.x = hit.position.x;
                            finalPosition.z = hit.position.z;
                            finalPosition.y = height; // preserve original height
                        }

                        // Prevent overlap with nearby trees
                        bool tooClose = false;
                        foreach (var pos in placedTreePositions)
                        {
                            if (Vector3.SqrMagnitude(pos - finalPosition) < minTreeSpacing * minTreeSpacing)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (tooClose)
                            continue;

                        biomeIndex = 0; // You can customize biome logic

                        GameObject tree = Instantiate(treePrefab[biomeIndex], finalPosition, Quaternion.identity);
                        tree.transform.parent = treeList[biomeIndex].transform;

                        float randomScale = Random.Range(0.2f, 1f);
                        tree.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
                        tree.transform.Rotate(0, Random.Range(0f, 360f), 0f, Space.Self);

                        placedTreePositions.Add(finalPosition); // Register this tree
                        treeCount++;
                    }
                }
            }
        }
    }
}
