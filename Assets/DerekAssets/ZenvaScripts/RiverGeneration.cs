using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RiverGeneration : MonoBehaviour
{
    [SerializeField]
    public GameObject riverList;

    [SerializeField]
    private int numberOfRivers;

    [SerializeField]
    private float heightThreshold;

    [SerializeField]
    private int looplimit = 200;

    [SerializeField]
    private float depressAmount = 5f;

    [SerializeField]
    private float riverPersistence = 1.5f;

    [SerializeField]
    private float waterHeight = 2f;

    [SerializeField]
    private float riverWidth = 20f;

    [SerializeField]
    private float riverYOffset = 10f;

    [SerializeField]
    private Material riverMaterial;

    [SerializeField]
    private bool enableDebugLogging = false;

    private const int RIVER_LOD = 0;
    private const int RIVER_SKIP_INCREMENT = 1;

    private float averageHeight;
    (Vector2, Vector2, Vector2, Vector2) updatedTuple = (Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
    private Vector2 skipDirection = new Vector2(0, 0);

    private HeightMap[,] riverchunksData;
    private Vector2 previousRiverPoint = Vector2.zero;
    int levelWidth;
    int levelDepth;
    int verticesPerLine;

    public void GenerateRivers(int tilesWidth, int verticesWidth, TerrainData terrainData)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[RIVER] Starting generation: {numberOfRivers} rivers on {tilesWidth}x{tilesWidth} tiles, {verticesWidth} vertices per tile");
            Debug.Log($"[RIVER] Mesh boundaries: vertices 1 to {verticesWidth - 2} are in mesh, 0 and {verticesWidth - 1} are borders only");
        }

        this.levelDepth = tilesWidth;
        this.levelWidth = tilesWidth;
        this.verticesPerLine = verticesWidth;
        riverchunksData = terrainData.chunksData;

        for (int riverIndex = 0; riverIndex < numberOfRivers; riverIndex++)
        {
            if (enableDebugLogging)
                Debug.Log($"[RIVER] === Generating river {riverIndex + 1}/{numberOfRivers} ===");

            (Vector2, Vector2, Vector2, Vector2) riverOrigin = ChooseRiverOrigin(tilesWidth, verticesWidth, terrainData);
            averageHeight = GetAverageHeight(riverOrigin, terrainData);

            if (enableDebugLogging)
                Debug.Log($"[RIVER] River {riverIndex + 1} origin: center({GetQuadCenter(riverOrigin)}) height: {averageHeight:F2}");

            BuildRiverPathAndCreateMesh(riverOrigin, verticesWidth, terrainData);
        }

        riverList.transform.Translate(new Vector3(0.0f, 0.2f, 0.0f));

        if (enableDebugLogging)
            Debug.Log("[RIVER] All rivers generated successfully");
    }

    private Vector2 GetQuadCenter((Vector2, Vector2, Vector2, Vector2) quad)
    {
        return new Vector2(
            (quad.Item1.x + quad.Item2.x + quad.Item3.x + quad.Item4.x) / 4,
            (quad.Item1.y + quad.Item2.y + quad.Item3.y + quad.Item4.y) / 4
        );
    }

    private (Vector2, Vector2, Vector2, Vector2) ChooseRiverOrigin(int tilesWidth, int verticesWidth, TerrainData terrainData)
    {
        int totalLevelWidth = tilesWidth * verticesWidth;
        int totalLevelDepth = tilesWidth * verticesWidth;
        int attempts = 0;
        const int maxAttempts = 1000;
        bool found = false;
        int randomXIndex = 50, randomZIndex = 150;

        while (!found && attempts < maxAttempts)
        {
            attempts++;

            // REMOVED RESTRICTION: Allow rivers to start anywhere, including tile boundaries
            randomXIndex = Random.Range(5, totalLevelWidth - 5);
            randomZIndex = Random.Range(5, totalLevelDepth - 5);

            TileCoordinate tileCoordinate = terrainData.ConvertToTileCoordinate(randomXIndex, randomZIndex);

            if (!IsValidTileCoordinate(tileCoordinate, terrainData))
                continue;

            HeightMap riverheightMap = terrainData.chunksData[tileCoordinate.tileXIndex, tileCoordinate.tileZIndex];
            float heightValue = riverheightMap.heightvalues[tileCoordinate.coordinateXIndex, tileCoordinate.coordinateZIndex];

            if (heightValue >= this.heightThreshold)
            {
                found = true;
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] Found origin after {attempts} attempts at world({randomXIndex}, {randomZIndex}) height: {heightValue:F2}");
            }
        }

        if (!found && enableDebugLogging)
        {
            Debug.LogWarning($"[RIVER] Could not find suitable origin after {maxAttempts} attempts. Using fallback.");
        }

        Vector2 firstOrigin = new Vector2(randomXIndex, randomZIndex);
        Vector2 secondOrigin = new Vector2(randomXIndex + 1, randomZIndex);
        Vector2 thirdOrigin = new Vector2(randomXIndex, randomZIndex + 1);
        Vector2 fourthOrigin = new Vector2(randomXIndex + 1, randomZIndex + 1);

        return (firstOrigin, secondOrigin, thirdOrigin, fourthOrigin);
    }

    private void BuildRiverPathAndCreateMesh((Vector2, Vector2, Vector2, Vector2) riverOrigin, int verticesWidth, TerrainData terrainData)
    {
        bool foundWater = false;
        int loopcount = 0;
        HashSet<Vector2> visitedCoordinates = new HashSet<Vector2>();
        List<Vector2> riverPathCenters = new List<Vector2>();

        List<Vector2> currentRiverList = new List<Vector2> { riverOrigin.Item1, riverOrigin.Item2, riverOrigin.Item3, riverOrigin.Item4 };
        visitedCoordinates.UnionWith(currentRiverList);

        Vector2 centerPoint = GetQuadCenter(riverOrigin);
        riverPathCenters.Add(centerPoint);

        (Vector2 firstOrigin, Vector2 secondOrigin, Vector2 thirdOrigin, Vector2 fourthOrigin) currentCoordinate = riverOrigin;

        while (!foundWater && loopcount < looplimit)
        {
            List<(Vector2, Vector2, Vector2, Vector2)> neighboringTuples = new List<(Vector2, Vector2, Vector2, Vector2)>();

            // Calculate neighboring 2x2 quads
            (Vector2, Vector2, Vector2, Vector2) aboveTuple = (
                currentCoordinate.firstOrigin + Vector2.up * 2,
                currentCoordinate.secondOrigin + Vector2.up * 2,
                currentCoordinate.thirdOrigin + Vector2.up * 2,
                currentCoordinate.fourthOrigin + Vector2.up * 2
            );

            (Vector2, Vector2, Vector2, Vector2) belowTuple = (
                currentCoordinate.firstOrigin + Vector2.down * 2,
                currentCoordinate.secondOrigin + Vector2.down * 2,
                currentCoordinate.thirdOrigin + Vector2.down * 2,
                currentCoordinate.fourthOrigin + Vector2.down * 2
            );

            (Vector2, Vector2, Vector2, Vector2) leftTuple = (
                currentCoordinate.firstOrigin + Vector2.left * 2,
                currentCoordinate.secondOrigin + Vector2.left * 2,
                currentCoordinate.thirdOrigin + Vector2.left * 2,
                currentCoordinate.fourthOrigin + Vector2.left * 2
            );

            (Vector2, Vector2, Vector2, Vector2) rightTuple = (
                currentCoordinate.firstOrigin + Vector2.right * 2,
                currentCoordinate.secondOrigin + Vector2.right * 2,
                currentCoordinate.thirdOrigin + Vector2.right * 2,
                currentCoordinate.fourthOrigin + Vector2.right * 2
            );

            // Validate neighbors
            if (IsValidQuadForMesh(aboveTuple, terrainData, visitedCoordinates)) neighboringTuples.Add(aboveTuple);
            if (IsValidQuadForMesh(leftTuple, terrainData, visitedCoordinates)) neighboringTuples.Add(leftTuple);
            if (IsValidQuadForMesh(rightTuple, terrainData, visitedCoordinates)) neighboringTuples.Add(rightTuple);
            if (IsValidQuadForMesh(belowTuple, terrainData, visitedCoordinates)) neighboringTuples.Add(belowTuple);

            float minAverageHeight = float.MaxValue;
            (Vector2, Vector2, Vector2, Vector2) minNeighborTuple = (Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            bool hasNeighbor = false;

            foreach ((Vector2, Vector2, Vector2, Vector2) neighborTuple in neighboringTuples)
            {
                float neighborAverageHeight = GetAverageHeight(neighborTuple, terrainData);
                if (neighborAverageHeight < minAverageHeight)
                {
                    hasNeighbor = true;
                    minAverageHeight = neighborAverageHeight;
                    minNeighborTuple = neighborTuple;
                }
            }

            if (averageHeight <= waterHeight)
            {
                foundWater = true;
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] Reached water at step {loopcount}, height: {averageHeight:F2}");
            }

            if (hasNeighbor && !foundWater)
            {
                Vector2 nextCenterPoint = GetQuadCenter(minNeighborTuple);
                riverPathCenters.Add(nextCenterPoint);

                currentCoordinate = minNeighborTuple;
                averageHeight = minAverageHeight;
                currentRiverList = new List<Vector2> { minNeighborTuple.Item1, minNeighborTuple.Item2, minNeighborTuple.Item3, minNeighborTuple.Item4 };
                visitedCoordinates.UnionWith(currentRiverList);
                loopcount++;
            }
            else
            {
                if (enableDebugLogging)
                {
                    string reason = foundWater ? "reached water" : "no valid neighbors";
                    Debug.Log($"[RIVER] River ended at step {loopcount}: {reason}");
                }

                // Depress terrain at all visited coordinates with proper tile boundary handling
                int depressedCount = 0;
                foreach (Vector2 visitedCoord in visitedCoordinates)
                {
                    if (DepressTerrainAtCoordinate(visitedCoord, terrainData, depressAmount))
                        depressedCount++;
                }

                if (enableDebugLogging)
                    Debug.Log($"[RIVER] Depressed {depressedCount} coordinates across tile boundaries");

                break;
            }
        }

        if (enableDebugLogging)
            Debug.Log($"[RIVER] River path complete: {riverPathCenters.Count} segments, {visitedCoordinates.Count} vertices");

        if (riverPathCenters.Count > 1)
        {
            GameObject riverMesh = CreateRiverMeshFromCenters(riverPathCenters, visitedCoordinates, terrainData);
            if (riverMesh != null)
            {
                riverMesh.transform.parent = riverList.transform;
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] Successfully created river mesh");
            }
        }
        else if (enableDebugLogging)
        {
            Debug.LogWarning($"[RIVER] Insufficient path points for mesh: {riverPathCenters.Count}");
        }
    }

    private bool DepressTerrainAtCoordinate(Vector2 coordinate, TerrainData terrainData, float depressAmount)
    {
        int x = (int)coordinate.x;
        int z = (int)coordinate.y;

        List<TileCoordinate> affectedTiles = GetAllTilesContainingCoordinate(x, z, terrainData);

        if (affectedTiles.Count == 0)
        {
            if (enableDebugLogging)
                Debug.LogWarning($"[RIVER] No tiles found for coordinate ({x}, {z})");
            return false;
        }

        TileCoordinate primaryTile = affectedTiles[0];
        if (!IsValidTileCoordinate(primaryTile, terrainData))
            return false;

        HeightMap primaryHeightMap = riverchunksData[primaryTile.tileXIndex, primaryTile.tileZIndex];
        float originalHeight = primaryHeightMap.heightvalues[primaryTile.coordinateXIndex, primaryTile.coordinateZIndex];
        float newHeight = originalHeight - depressAmount;

        // Enhanced boundary detection logging
        if (enableDebugLogging)
        {
            int localX = primaryTile.coordinateXIndex;
            int localZ = primaryTile.coordinateZIndex;
            bool isAtTileBoundary = (localX == 1) || (localX == verticesPerLine - 2) || (localZ == 1) || (localZ == verticesPerLine - 2);

            if (affectedTiles.Count > 1 || isAtTileBoundary)
            {
                Debug.Log($"[RIVER] BOUNDARY: World({x}, {z}) -> Tile({primaryTile.tileXIndex}, {primaryTile.tileZIndex}) Local({localX}, {localZ}) - {affectedTiles.Count} tiles affected");
            }
        }

        // Update height in ALL tiles that contain this vertex
        foreach (TileCoordinate tileCoord in affectedTiles)
        {
            if (IsValidTileCoordinate(tileCoord, terrainData))
            {
                terrainData.chunksData[tileCoord.tileXIndex, tileCoord.tileZIndex].heightvalues[tileCoord.coordinateXIndex, tileCoord.coordinateZIndex] = newHeight;
            }
        }

        return true;
    }

    private List<TileCoordinate> GetAllTilesContainingCoordinate(int worldX, int worldZ, TerrainData terrainData)
    {
        List<TileCoordinate> containingTiles = new List<TileCoordinate>();

        TileCoordinate primaryTile = terrainData.ConvertToTileCoordinate(worldX, worldZ);
        if (!IsValidTileCoordinate(primaryTile, terrainData))
            return containingTiles;

        containingTiles.Add(primaryTile);

        int localX = primaryTile.coordinateXIndex;
        int localZ = primaryTile.coordinateZIndex;

        // Right edge mesh boundary: when localX == 123
        if (localX == 123)
        {
            TileCoordinate rightTile = terrainData.ConvertToTileCoordinate(worldX + 3, worldZ);
            if (IsValidTileCoordinate(rightTile, terrainData) && rightTile.coordinateXIndex == 1)
            {
                containingTiles.Add(rightTile);
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] OVERLAP DETECTED: World({worldX},{worldZ}) overlaps with World({worldX + 3},{worldZ})!");
            }
        }

        // Left edge mesh boundary: when localX == 1
        if (localX == 1)
        {
            TileCoordinate leftTile = terrainData.ConvertToTileCoordinate(worldX - 3, worldZ);
            if (IsValidTileCoordinate(leftTile, terrainData) && leftTile.coordinateXIndex == 123)
            {
                containingTiles.Add(leftTile);
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] OVERLAP DETECTED: World({worldX},{worldZ}) overlaps with World({worldX - 3},{worldZ})!");
            }
        }

        // Similar patterns for Z coordinates
        if (localZ == 123)
        {
            TileCoordinate topTile = terrainData.ConvertToTileCoordinate(worldX, worldZ - 3);
            if (IsValidTileCoordinate(topTile, terrainData) && topTile.coordinateZIndex == 1)
            {
                containingTiles.Add(topTile);
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] OVERLAP DETECTED: World({worldX},{worldZ}) overlaps with World({worldX},{worldZ - 3})!");
            }
        }

        if (localZ == 1)
        {
            TileCoordinate bottomTile = terrainData.ConvertToTileCoordinate(worldX, worldZ + 3);
            if (IsValidTileCoordinate(bottomTile, terrainData) && bottomTile.coordinateZIndex == 123)
            {
                containingTiles.Add(bottomTile);
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] OVERLAP DETECTED: World({worldX},{worldZ}) overlaps with World({worldX},{worldZ + 3})!");
            }
        }

        // Corner overlaps
        if (localX == 123 && localZ == 123)
        {
            TileCoordinate cornerTile = terrainData.ConvertToTileCoordinate(worldX + 3, worldZ - 3);
            if (IsValidTileCoordinate(cornerTile, terrainData) && cornerTile.coordinateXIndex == 1 && cornerTile.coordinateZIndex == 1)
            {
                containingTiles.Add(cornerTile);
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] CORNER OVERLAP: World({worldX},{worldZ}) overlaps with World({worldX + 3},{worldZ - 3})!");
            }
        }

        if (localX == 1 && localZ == 1)
        {
            TileCoordinate cornerTile = terrainData.ConvertToTileCoordinate(worldX - 3, worldZ + 3);
            if (IsValidTileCoordinate(cornerTile, terrainData) && cornerTile.coordinateXIndex == 123 && cornerTile.coordinateZIndex == 123)
            {
                containingTiles.Add(cornerTile);
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] CORNER OVERLAP: World({worldX},{worldZ}) overlaps with World({worldX - 3},{worldZ + 3})!");
            }
        }

        if (localX == 1 && localZ == 123)
        {
            TileCoordinate cornerTile = terrainData.ConvertToTileCoordinate(worldX - 3, worldZ - 3);
            if (IsValidTileCoordinate(cornerTile, terrainData) && cornerTile.coordinateXIndex == 123 && cornerTile.coordinateZIndex == 1)
            {
                containingTiles.Add(cornerTile);
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] CORNER OVERLAP: World({worldX},{worldZ}) overlaps with World({worldX - 3},{worldZ - 3})!");
            }
        }

        if (localX == 123 && localZ == 1)
        {
            TileCoordinate cornerTile = terrainData.ConvertToTileCoordinate(worldX + 3, worldZ + 3);
            if (IsValidTileCoordinate(cornerTile, terrainData) && cornerTile.coordinateXIndex == 1 && cornerTile.coordinateZIndex == 123)
            {
                containingTiles.Add(cornerTile);
                if (enableDebugLogging)
                    Debug.Log($"[RIVER] CORNER OVERLAP: World({worldX},{worldZ}) overlaps with World({worldX + 3},{worldZ + 3})!");
            }
        }

        return containingTiles;
    }

    private bool IsValidQuadForMesh((Vector2, Vector2, Vector2, Vector2) quad, TerrainData terrainData, HashSet<Vector2> visitedCoordinates)
    {
        List<Vector2> quadVertices = new List<Vector2> { quad.Item1, quad.Item2, quad.Item3, quad.Item4 };

        foreach (Vector2 vertex in quadVertices)
        {
            if (visitedCoordinates.Contains(vertex))
                return false;

            if (!IsBasicValidCoordinate(vertex, terrainData))
                return false;
        }

        return true;
    }

    private bool IsBasicValidCoordinate(Vector2 coordinate, TerrainData terrainData)
    {
        int totalLevelWidth = terrainData.levelDepthInTiles * verticesPerLine;
        int totalLevelDepth = terrainData.levelDepthInTiles * verticesPerLine;

        return coordinate.x >= 0 && coordinate.x < totalLevelWidth &&
               coordinate.y >= 0 && coordinate.y < totalLevelDepth;
    }

    private bool IsValidTileCoordinate(TileCoordinate tileCoord, TerrainData terrainData)
    {
        return tileCoord.tileXIndex >= 0 && tileCoord.tileXIndex < terrainData.levelDepthInTiles &&
               tileCoord.tileZIndex >= 0 && tileCoord.tileZIndex < terrainData.levelDepthInTiles &&
               tileCoord.coordinateXIndex >= 0 && tileCoord.coordinateXIndex < verticesPerLine &&
               tileCoord.coordinateZIndex >= 0 && tileCoord.coordinateZIndex < verticesPerLine;
    }

    // CRITICAL FIX: Transform world coordinates to match terrain mesh positioning
    private Vector3 WorldToMeshPosition(Vector2 worldCoordinate, TerrainData terrainData)
    {
        int worldX = (int)worldCoordinate.x;
        int worldZ = (int)worldCoordinate.y;

        // Convert to tile coordinate to get the transformation info
        TileCoordinate tileCoord = terrainData.ConvertToTileCoordinate(worldX, worldZ);

        // Get the height at this position
        float height = GetTerrainHeightAtPoint(worldCoordinate, terrainData);

        // Apply the same transformation logic as in TreeGeneration.cs
        // Based on your tree generation, the mesh position calculation is:
        // X: worldX - 1 - (tileXIndex * 3)
        // Z: (tileZIndex + 1) * (verticesPerLine - 3) - (coordinateZIndex - 4)

        float meshX = worldX - 1 - (tileCoord.tileXIndex * 3);
        float meshZ = (tileCoord.tileZIndex + 1) * (verticesPerLine - 3) - (tileCoord.coordinateZIndex - 4);

        if (enableDebugLogging)
        {
            Debug.Log($"[RIVER] World({worldX}, {worldZ}) -> Tile({tileCoord.tileXIndex}, {tileCoord.tileZIndex}) " +
                     $"Local({tileCoord.coordinateXIndex}, {tileCoord.coordinateZIndex}) -> Mesh({meshX:F2}, {meshZ:F2})");
        }

        return new Vector3(meshX, height, meshZ);
    }

    // Create a proper river spine with correctly handled turns
    private List<Vector3> CreateRiverSpine(List<Vector2> riverPathCenters, TerrainData terrainData)
    {
        if (riverPathCenters.Count < 2)
            return new List<Vector3>();

        List<Vector3> spine = new List<Vector3>();

        // Convert all path centers to mesh positions - keep it simple first
        foreach (Vector2 center in riverPathCenters)
        {
            Vector3 meshPos = WorldToMeshPosition(center, terrainData);
            spine.Add(meshPos);
        }

        return spine;
    }

    // Generate river cross-section points for proper mesh generation
    private struct RiverCrossSection
    {
        public Vector3 center;
        public Vector3 leftBank;
        public Vector3 rightBank;
        public Vector3 direction;
        public Vector3 normal;
    }

    private List<RiverCrossSection> GenerateRiverCrossSections(List<Vector3> spine)
    {
        List<RiverCrossSection> sections = new List<RiverCrossSection>();

        if (spine.Count < 2)
            return sections;

        for (int i = 0; i < spine.Count; i++)
        {
            Vector3 direction;

            // Calculate direction based on position in spine
            if (i == 0)
            {
                // First point: use direction to next point
                direction = (spine[i + 1] - spine[i]).normalized;
            }
            else if (i == spine.Count - 1)
            {
                // Last point: use direction from previous point
                direction = (spine[i] - spine[i - 1]).normalized;
            }
            else
            {
                // Middle points: average of incoming and outgoing directions for smooth turns
                Vector3 incoming = (spine[i] - spine[i - 1]).normalized;
                Vector3 outgoing = (spine[i + 1] - spine[i]).normalized;
                direction = (incoming + outgoing).normalized;
            }

            // Calculate perpendicular vector (river width direction)
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(direction, up).normalized;

            // Create cross-section
            RiverCrossSection section = new RiverCrossSection();
            section.center = spine[i];
            section.direction = direction;
            section.normal = right;
            section.leftBank = spine[i] - right * (riverWidth / 2);
            section.rightBank = spine[i] + right * (riverWidth / 2);

            // Apply Y offset
            section.center.y += riverYOffset;
            section.leftBank.y += riverYOffset;
            section.rightBank.y += riverYOffset;

            sections.Add(section);
        }

        return sections;
    }

    private GameObject CreateRiverMeshFromCenters(List<Vector2> riverPathCenters, HashSet<Vector2> visitedCoordinates, TerrainData terrainData)
    {
        if (riverPathCenters.Count < 2)
            return null;

        GameObject riverObject = new GameObject("River");
        MeshFilter meshFilter = riverObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = riverObject.AddComponent<MeshRenderer>();

        if (riverMaterial != null)
            meshRenderer.material = riverMaterial;

        // Create river spine and cross-sections
        List<Vector3> spine = CreateRiverSpine(riverPathCenters, terrainData);
        List<RiverCrossSection> sections = GenerateRiverCrossSections(spine);

        if (sections.Count < 2)
        {
            Debug.LogWarning("[RIVER] Insufficient river sections");
            return null;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Generate mesh from cross-sections
        for (int i = 0; i < sections.Count; i++)
        {
            RiverCrossSection section = sections[i];

            // Add vertices for this cross-section (left bank, center, right bank)
            vertices.Add(section.leftBank);
            vertices.Add(section.center);
            vertices.Add(section.rightBank);

            // Add UVs
            float v = (float)i / (sections.Count - 1);
            uvs.Add(new Vector2(0, v));    // Left bank
            uvs.Add(new Vector2(0.5f, v)); // Center
            uvs.Add(new Vector2(1, v));    // Right bank

            // Generate triangles and segment obstacles
            if (i < sections.Count - 1)
            {
                int baseIndex = i * 3;
                int nextBaseIndex = (i + 1) * 3;

                // Left triangle strip
                triangles.Add(baseIndex);         // Current left
                triangles.Add(baseIndex + 1);     // Current center
                triangles.Add(nextBaseIndex);     // Next left

                triangles.Add(nextBaseIndex);     // Next left
                triangles.Add(baseIndex + 1);     // Current center
                triangles.Add(nextBaseIndex + 1); // Next center

                // Right triangle strip
                triangles.Add(baseIndex + 1);     // Current center
                triangles.Add(baseIndex + 2);     // Current right
                triangles.Add(nextBaseIndex + 1); // Next center

                triangles.Add(nextBaseIndex + 1); // Next center
                triangles.Add(baseIndex + 2);     // Current right
                triangles.Add(nextBaseIndex + 2); // Next right

                // Create NavMeshObstacle for this segment
                CreateObstacleForSegment(riverObject.transform, section, sections[i + 1]);
            }
        }

        // Create and assign mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        riverObject.AddComponent<MeshCollider>();
        riverObject.layer = LayerMask.NameToLayer("Obstacle");

        if (enableDebugLogging)
        {
            Debug.Log($"[RIVER] Created river mesh with {vertices.Count} vertices, {triangles.Count / 3} triangles from {sections.Count} cross-sections");
        }

        return riverObject;
    }

    private void CreateObstacleForSegment(Transform parent, RiverCrossSection a, RiverCrossSection b)
    {
        Vector3 centerA = a.center;
        Vector3 centerB = b.center;

        Vector3 segmentDirection = (centerB - centerA).normalized;
        float segmentLength = Vector3.Distance(centerA, centerB);
        float segmentWidth = Vector3.Distance(a.leftBank, a.rightBank);

        Vector3 segmentCenter = (centerA + centerB) / 2f;

        GameObject obstacleObj = new GameObject("RiverObstacleSegment");
        obstacleObj.transform.parent = parent;
        obstacleObj.transform.position = segmentCenter;

        // Orient the box along the river
        obstacleObj.transform.rotation = Quaternion.LookRotation(segmentDirection, Vector3.up);

        // Add obstacle
        NavMeshObstacle obstacle = obstacleObj.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.carving = true;

        // Set size: width (X), height (Y), length (Z)
        obstacle.size = new Vector3(segmentWidth, 2f, segmentLength);

        // Optional: match river layer
        obstacleObj.layer = LayerMask.NameToLayer("Obstacle");
    }

    private float GetTerrainHeightAtPoint(Vector2 coordinate, TerrainData terrainData)
    {
        int x = (int)coordinate.x;
        int z = (int)coordinate.y;

        List<TileCoordinate> containingTiles = GetAllTilesContainingCoordinate(x, z, terrainData);

        if (containingTiles.Count == 0)
            return 0f;

        float totalHeight = 0f;
        int validTiles = 0;

        foreach (TileCoordinate tileCoord in containingTiles)
        {
            if (IsValidTileCoordinate(tileCoord, terrainData))
            {
                HeightMap heightMap = terrainData.chunksData[tileCoord.tileXIndex, tileCoord.tileZIndex];
                totalHeight += heightMap.heightvalues[tileCoord.coordinateXIndex, tileCoord.coordinateZIndex];
                validTiles++;
            }
        }

        return validTiles > 0 ? totalHeight / validTiles : 0f;
    }

    private float GetAverageHeight((Vector2, Vector2, Vector2, Vector2) currentCoordinate, TerrainData terrainData)
    {
        float totalHeight = 0f;
        int count = 0;

        foreach (Vector2 coord in new List<Vector2> { currentCoordinate.Item1, currentCoordinate.Item2, currentCoordinate.Item3, currentCoordinate.Item4 })
        {
            float height = GetTerrainHeightAtPoint(coord, terrainData);
            totalHeight += height;
            count++;
        }

        return count > 0 ? totalHeight / count : 0f;
    }
}