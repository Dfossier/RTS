using System.Collections.Generic;
using UnityEngine;

public class RiverGeneration : MonoBehaviour
{
    [SerializeField] private GameObject riverList; // Parent object for river meshes
    [SerializeField] private int numberOfRivers = 1; // Number of rivers to generate
    [SerializeField] private float heightThreshold = 10f; // Minimum height for river origin
    [SerializeField] private int loopLimit = 200; // Maximum iterations for river path
    [SerializeField] private float depressAmount = 5f; // Amount to depress terrain for riverbed
    [SerializeField] private float riverPersistence = 1.5f; // Not used currently, kept for future
    [SerializeField] private float waterHeight = 2f; // Height at which river stops
    [SerializeField] private float riverWidth = 2f; // Width of the river mesh
    [SerializeField] private float waterOffset = 0.4f; // Offset below original terrain height for river mesh

    private HeightMap[,] riverChunksData;
    private Vector2 previousRiverPoint = Vector2.zero;
    private int levelWidth;
    private int levelDepth;
    private (Vector2, Vector2, Vector2, Vector2) updatedTuple = (Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
    private Vector2 skipDirection = Vector2.zero;

    public void GenerateRivers(int tilesWidth, int verticesWidth, TerrainData terrainData)
    {
        levelDepth = tilesWidth;
        levelWidth = verticesWidth;
        riverChunksData = terrainData.chunksData;

        for (int riverIndex = 0; riverIndex < numberOfRivers; riverIndex++)
        {
            var riverOrigin = ChooseRiverOrigin(tilesWidth, verticesWidth, terrainData);
            float averageHeight = GetAverageHeight(riverOrigin, terrainData);
            List<(Vector3 position, float originalHeight)> riverPoints = BuildRiver(riverOrigin, verticesWidth, terrainData, averageHeight);

            if (riverPoints.Count > 1)
            {
                GenerateRiverMesh(riverPoints, riverIndex);
            }
        }
    }

    private (Vector2, Vector2, Vector2, Vector2) ChooseRiverOrigin(int tilesWidth, int verticesWidth, TerrainData terrainData)
    {
        bool found = false;
        int randomXIndex = 50;
        int randomZIndex = 150;

        while (!found)
        {
            randomXIndex = Random.Range(0, tilesWidth * verticesWidth - 8);
            randomZIndex = Random.Range(0, tilesWidth * verticesWidth - 8);
            TileCoordinate tileCoordinate = terrainData.ConvertToTileCoordinate(randomXIndex, randomZIndex);
            HeightMap heightMap = riverChunksData[tileCoordinate.tileXIndex, tileCoordinate.tileZIndex];
            float heightValue = heightMap.heightvalues[tileCoordinate.coordinateXIndex, tileCoordinate.coordinateZIndex];

            if (heightValue >= heightThreshold)
            {
                found = true;
            }
        }

        return (
            new Vector2(randomXIndex, randomZIndex),
            new Vector2(randomXIndex + 1, randomZIndex),
            new Vector2(randomXIndex, randomZIndex + 1),
            new Vector2(randomXIndex + 1, randomZIndex + 1)
        );
    }

    private List<(Vector3 position, float originalHeight)> BuildRiver((Vector2, Vector2, Vector2, Vector2) riverOrigin, int verticesWidth, TerrainData terrainData, float initialAverageHeight)
    {
        bool foundWater = false;
        int loopCount = 0;
        HashSet<Vector2> visitedCoordinates = new HashSet<Vector2>();
        List<(Vector3 position, float originalHeight)> riverPoints = new List<(Vector3, float)>();
        var currentCoordinate = riverOrigin;
        float averageHeight = initialAverageHeight;

        visitedCoordinates.Add(riverOrigin.Item1);
        visitedCoordinates.Add(riverOrigin.Item2);
        visitedCoordinates.Add(riverOrigin.Item3);
        visitedCoordinates.Add(riverOrigin.Item4);

        TileCoordinate initialCoord = terrainData.ConvertToTileCoordinate((int)riverOrigin.Item1.x, (int)riverOrigin.Item1.y);
        HeightMap initialHeightMap = riverChunksData[initialCoord.tileXIndex, initialCoord.tileZIndex];
        float initialHeight = initialHeightMap.heightvalues[initialCoord.coordinateXIndex, initialCoord.coordinateZIndex];
        riverPoints.Add((new Vector3(riverOrigin.Item1.x, initialHeight, riverOrigin.Item1.y), initialHeight));

        while (!foundWater && loopCount < loopLimit)
        {
            List<(Vector2, Vector2, Vector2, Vector2)> neighboringTuples = new List<(Vector2, Vector2, Vector2, Vector2)>();
            var aboveTuple = (currentCoordinate.Item3, currentCoordinate.Item4, currentCoordinate.Item3 + Vector2.up, currentCoordinate.Item4 + Vector2.up);
            var belowTuple = (currentCoordinate.Item1 + Vector2.down, currentCoordinate.Item2 + Vector2.down, currentCoordinate.Item1, currentCoordinate.Item2);
            var leftTuple = (currentCoordinate.Item1 + Vector2.left, currentCoordinate.Item1, currentCoordinate.Item3 + Vector2.left, currentCoordinate.Item3);
            var rightTuple = (currentCoordinate.Item2, currentCoordinate.Item2 + Vector2.right, currentCoordinate.Item4, currentCoordinate.Item4 + Vector2.right);

            if (!visitedCoordinates.Contains(aboveTuple.Item3) && !visitedCoordinates.Contains(aboveTuple.Item4) &&
                IsValidCoordinate(aboveTuple.Item3, verticesWidth, terrainData) && IsValidCoordinate(aboveTuple.Item4, verticesWidth, terrainData))
            {
                neighboringTuples.Add(aboveTuple);
            }
            if (!visitedCoordinates.Contains(leftTuple.Item1) && !visitedCoordinates.Contains(leftTuple.Item3) &&
                IsValidCoordinate(leftTuple.Item1, verticesWidth, terrainData) && IsValidCoordinate(leftTuple.Item3, verticesWidth, terrainData))
            {
                neighboringTuples.Add(leftTuple);
            }
            if (!visitedCoordinates.Contains(rightTuple.Item2) && !visitedCoordinates.Contains(rightTuple.Item4) &&
                IsValidCoordinate(rightTuple.Item2, verticesWidth, terrainData) && IsValidCoordinate(rightTuple.Item4, verticesWidth, terrainData))
            {
                neighboringTuples.Add(rightTuple);
            }
            if (!visitedCoordinates.Contains(belowTuple.Item1) && !visitedCoordinates.Contains(belowTuple.Item2) &&
                IsValidCoordinate(belowTuple.Item1, verticesWidth, terrainData) && IsValidCoordinate(belowTuple.Item2, verticesWidth, terrainData))
            {
                neighboringTuples.Add(belowTuple);
            }

            float minAverageHeight = float.MaxValue;
            (Vector2, Vector2, Vector2, Vector2) minNeighborTuple = (Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            bool hasNeighbor = false;

            foreach (var neighborTuple in neighboringTuples)
            {
                float neighborAverageHeight = GetAverageHeight(neighborTuple, terrainData);
                if (neighborAverageHeight < minAverageHeight)
                {
                    hasNeighbor = true;
                    minAverageHeight = neighborAverageHeight;
                    minNeighborTuple = neighborTuple;
                }
            }

            if (IsSkippedTuple(currentCoordinate))
            {
                updatedTuple = (
                    currentCoordinate.Item1 + skipDirection,
                    currentCoordinate.Item2 + skipDirection,
                    currentCoordinate.Item3 + skipDirection,
                    currentCoordinate.Item4 + skipDirection
                );
                minNeighborTuple = updatedTuple;
            }

            skipDirection = minNeighborTuple.Item1 - currentCoordinate.Item1;

            if (averageHeight <= waterHeight || !hasNeighbor)
            {
                foundWater = true;
            }
            else
            {
                TileCoordinate riverCoord = terrainData.ConvertToTileCoordinate((int)minNeighborTuple.Item1.x, (int)minNeighborTuple.Item1.y);
                HeightMap heightMap = riverChunksData[riverCoord.tileXIndex, riverCoord.tileZIndex];
                float originalHeight = heightMap.heightvalues[riverCoord.coordinateXIndex, riverCoord.coordinateZIndex];
                riverPoints.Add((new Vector3(minNeighborTuple.Item1.x, originalHeight, minNeighborTuple.Item1.y), originalHeight));

                currentCoordinate = minNeighborTuple;
                averageHeight = minAverageHeight;
                visitedCoordinates.Add(minNeighborTuple.Item1);
                visitedCoordinates.Add(minNeighborTuple.Item2);
                visitedCoordinates.Add(minNeighborTuple.Item3);
                visitedCoordinates.Add(minNeighborTuple.Item4);
                loopCount++;
            }
        }

        foreach (Vector2 visitedCoord in visitedCoordinates)
        {
            TileCoordinate visitedTileCoord = terrainData.ConvertToTileCoordinate((int)visitedCoord.x, (int)visitedCoord.y);
            HeightMap heightMap = riverChunksData[visitedTileCoord.tileXIndex, visitedTileCoord.tileZIndex];
            heightMap.heightvalues[visitedTileCoord.coordinateXIndex, visitedTileCoord.coordinateZIndex] -= depressAmount;
        }

        return riverPoints;
    }

    private void GenerateRiverMesh(List<(Vector3 position, float originalHeight)> riverPoints, int riverIndex)
    {
        if (riverPoints.Count < 2) return;

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < riverPoints.Count - 1; i++)
        {
            Vector3 pointA = riverPoints[i].position;
            float heightA = riverPoints[i].originalHeight - waterOffset;
            Vector3 pointB = riverPoints[i + 1].position;
            float heightB = riverPoints[i + 1].originalHeight - waterOffset;

            pointA.y = heightA;
            pointB.y = heightB;

            Vector3 direction = (pointB - pointA).normalized;
            Vector3 perp = Vector3.Cross(direction, Vector3.up).normalized * riverWidth * 0.5f;

            vertices.Add(pointA - perp); // Left vertex
            vertices.Add(pointA + perp); // Right vertex
            vertices.Add(pointB - perp); // Next left vertex
            vertices.Add(pointB + perp); // Next right vertex

            float u = (float)i / (riverPoints.Count - 1);
            uvs.Add(new Vector2(0, u));
            uvs.Add(new Vector2(1, u));
            uvs.Add(new Vector2(0, u + 1f / (riverPoints.Count - 1)));
            uvs.Add(new Vector2(1, u + 1f / (riverPoints.Count - 1)));

            // Add triangles with reversed winding order to make top side the front face
            int baseIndex = i * 4;
            triangles.Add(baseIndex + 0); triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 2); // First triangle (clockwise from top)
            triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 3); triangles.Add(baseIndex + 2); // Second triangle (clockwise from top)
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals(); // Ensure normals point upward based on new winding order

        GameObject riverObject = new GameObject($"River_{riverIndex}");
        riverObject.transform.parent = riverList.transform;
        MeshFilter meshFilter = riverObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = riverObject.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;

        Material riverMaterial = new Material(Shader.Find("Standard"));
        riverMaterial.color = Color.blue;
        meshRenderer.material = riverMaterial;
    }

    private float GetAverageHeight((Vector2, Vector2, Vector2, Vector2) currentCoordinate, TerrainData terrainData)
    {
        float totalHeight = 0f;
        int count = 0;
        foreach (Vector2 coord in new List<Vector2> { currentCoordinate.Item1, currentCoordinate.Item2, currentCoordinate.Item3, currentCoordinate.Item4 })
        {
            TileCoordinate tileCoord = terrainData.ConvertToTileCoordinate((int)coord.x, (int)coord.y);
            HeightMap heightMap = riverChunksData[tileCoord.tileXIndex, tileCoord.tileZIndex];
            totalHeight += heightMap.heightvalues[tileCoord.coordinateXIndex, tileCoord.coordinateZIndex];
            count++;
        }
        return count > 0 ? totalHeight / count : 0f;
    }

    private bool IsSkippedTuple((Vector2, Vector2, Vector2, Vector2) coordinateTuple)
    {
        foreach (Vector2 coordinate in new List<Vector2> { coordinateTuple.Item1, coordinateTuple.Item2, coordinateTuple.Item3, coordinateTuple.Item4 })
        {
            if (IsSkippedValue(coordinate.x) || IsSkippedValue(coordinate.y))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsSkippedValue(float value)
    {
        return value == 121 || value == 122 || value == 123 || value == 124 || value == 125;
    }

    private bool IsValidCoordinate(Vector2 coordinate, int verticesWidth, TerrainData terrainData)
    {
        TileCoordinate tileCoord = terrainData.ConvertToTileCoordinate((int)coordinate.x, (int)coordinate.y);
        return tileCoord.tileXIndex >= 0 && tileCoord.tileXIndex < terrainData.levelDepthInTiles &&
               tileCoord.tileZIndex >= 0 && tileCoord.tileZIndex < terrainData.levelDepthInTiles &&
               tileCoord.coordinateXIndex >= 0 && tileCoord.coordinateXIndex <= verticesWidth &&
               tileCoord.coordinateZIndex >= 0 && tileCoord.coordinateZIndex <= verticesWidth;
    }
}