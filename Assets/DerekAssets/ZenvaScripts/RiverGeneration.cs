using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RiverGeneration : MonoBehaviour
{
    public TerrainGenerator TerrainGenerator { get; private set; }
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

    public void SetTerrainGenerator(TerrainGenerator generator)
    {
        TerrainGenerator = generator;
    }

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

    // Build a grid-cell blob mesh so that looping paths form a natural lake rather than a
    // self-intersecting strip. Each path step expands outward by riverWidth/2 to fill a
    // disc of 2-unit cells. Adjacent cells share vertices (deduplication via dictionary),
    // producing a single connected water surface with no folded triangles.
    private GameObject CreateRiverMeshFromCenters(List<Vector2> riverPathCenters, HashSet<Vector2> visitedCoordinates, TerrainData terrainData)
    {
        if (riverPathCenters.Count < 1)
            return null;

        GameObject riverObject = new GameObject("River");
        MeshFilter meshFilter = riverObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = riverObject.AddComponent<MeshRenderer>();
        if (riverMaterial != null)
            meshRenderer.material = riverMaterial;

        int radiusCells = Mathf.Max(1, Mathf.RoundToInt(riverWidth / 2f / 2f));
        int radiusSq    = radiusCells * radiusCells;

        // Pre-compute mesh positions so we don't call WorldToMeshPosition twice per step.
        var pathMeshPos = new List<Vector3>(riverPathCenters.Count);
        foreach (Vector2 c in riverPathCenters)
            pathMeshPos.Add(WorldToMeshPosition(c, terrainData));

        // Fill cells in path order (source → mouth). First-claim wins, which naturally
        // gives high-elevation cells their source height and low-elevation cells their
        // mouth height, producing a downhill slope across the water surface.
        // The Y offset tapers from full → 0 over the last 25% of the path so the
        // river settles to terrain level at the mouth instead of hovering above it.
        var filledCells  = new HashSet<(int, int)>();
        var cellHeights  = new Dictionary<(int, int), float>();
        int lastStep = Mathf.Max(1, riverPathCenters.Count - 1);

        for (int pi = 0; pi < riverPathCenters.Count; pi++)
        {
            Vector3 mp = pathMeshPos[pi];
            int cx = Mathf.RoundToInt(mp.x / 2f);
            int cz = Mathf.RoundToInt(mp.z / 2f);
            float t      = (float)pi / lastStep;
            float taper  = Mathf.Clamp01((t - 0.75f) / 0.25f); // 0 for first 75%, ramps 0→1 in last 25%
            float h      = mp.y + Mathf.Lerp(riverYOffset, 0f, taper);

            for (int dx = -radiusCells; dx <= radiusCells; dx++)
            {
                for (int dz = -radiusCells; dz <= radiusCells; dz++)
                {
                    if (dx * dx + dz * dz <= radiusSq)
                    {
                        var key = (cx + dx, cz + dz);
                        if (!cellHeights.ContainsKey(key))
                        {
                            filledCells.Add(key);
                            cellHeights[key] = h;
                        }
                    }
                }
            }
        }

        // Dilate: fill concave staircase corners without shrinking the river.
        // An empty cell is a concave corner if it has a filled neighbour on at least one
        // horizontal axis AND at least one vertical axis — the exact shape of a notch.
        // We seed candidates from the diagonal neighbours of existing cells to avoid
        // scanning the whole map. Height is averaged from orthogonal filled neighbours.
        var seen    = new HashSet<(int, int)>();
        var toFill  = new List<(int, int)>();
        (int, int)[] diags = { (1, 1), (1, -1), (-1, 1), (-1, -1) };
        (int, int)[] ortho = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        foreach (var (cx, cz) in filledCells)
        {
            foreach (var (ddx, ddz) in diags)
            {
                var cand = (cx + ddx, cz + ddz);
                if (filledCells.Contains(cand) || !seen.Add(cand)) continue;

                bool hasX = filledCells.Contains((cand.Item1 + 1, cand.Item2))
                         || filledCells.Contains((cand.Item1 - 1, cand.Item2));
                bool hasZ = filledCells.Contains((cand.Item1, cand.Item2 + 1))
                         || filledCells.Contains((cand.Item1, cand.Item2 - 1));
                if (hasX && hasZ) toFill.Add(cand);
            }
        }
        foreach (var (cx, cz) in toFill)
        {
            float totalH = 0f; int cnt = 0;
            foreach (var (ddx, ddz) in ortho)
                if (cellHeights.TryGetValue((cx + ddx, cz + ddz), out float nh))
                { totalH += nh; cnt++; }
            filledCells.Add((cx, cz));
            cellHeights[(cx, cz)] = cnt > 0 ? totalH / cnt : riverYOffset;
        }

        // Accumulate height contributions from each filled cell to its 4 corners so
        // shared vertices get the average height of their surrounding cells — this
        // smooths the slope transitions across the surface.
        var vertexHeightAccum = new Dictionary<(int, int), (float sum, int count)>();
        foreach (var (cx, cz) in filledCells)
        {
            float h = cellHeights[(cx, cz)];
            foreach (var key in new (int, int)[] {
                (cx * 2, cz * 2), (cx * 2 + 2, cz * 2),
                (cx * 2 + 2, cz * 2 + 2), (cx * 2, cz * 2 + 2) })
            {
                if (vertexHeightAccum.TryGetValue(key, out var acc))
                    vertexHeightAccum[key] = (acc.sum + h, acc.count + 1);
                else
                    vertexHeightAccum[key] = (h, 1);
            }
        }

        var vertexMap = new Dictionary<(int, int), int>();
        var vertices  = new List<Vector3>();
        var triangles = new List<int>();
        var uvs       = new List<Vector2>();

        // Local helper: look up or create a vertex at grid position (vx, vz).
        int GetOrAddVertex((int, int) key)
        {
            if (vertexMap.TryGetValue(key, out int vi)) return vi;
            vi = vertices.Count;
            vertexMap[key] = vi;
            var (sum, count) = vertexHeightAccum.TryGetValue(key, out var acc)
                ? acc : (riverYOffset, 1);
            vertices.Add(new Vector3(key.Item1, sum / count, key.Item2));
            uvs.Add(new Vector2(key.Item1 / riverWidth, key.Item2 / riverWidth));
            return vi;
        }

        // Emit one quad per filled cell (two CW-from-above triangles).
        foreach (var (cx, cz) in filledCells)
        {
            int i0 = GetOrAddVertex((cx * 2,     cz * 2));
            int i1 = GetOrAddVertex((cx * 2 + 2, cz * 2));
            int i2 = GetOrAddVertex((cx * 2 + 2, cz * 2 + 2));
            int i3 = GetOrAddVertex((cx * 2,     cz * 2 + 2));

            triangles.Add(i0); triangles.Add(i2); triangles.Add(i1);
            triangles.Add(i0); triangles.Add(i3); triangles.Add(i2);
        }

        // Diagonal smoothing: fill staircase notches at the boundary.
        // A notch exists where two cells are diagonally adjacent but neither orthogonal
        // neighbor between them is filled. Two bridging triangles cut each notch to 45°.
        // Only checking NE (1,1) and SE (1,-1) per cell avoids processing each pair twice.
        foreach (var (cx, cz) in filledCells)
        {
            // NE diagonal: shared corner is TR of (cx,cz) = (cx*2+2, cz*2+2)
            if ( filledCells.Contains((cx + 1, cz + 1)) &&
                !filledCells.Contains((cx + 1, cz)) &&
                !filledCells.Contains((cx,     cz + 1)))
            {
                int vx = cx * 2 + 2, vz = cz * 2 + 2;
                // SE notch — covers the (cx+1, cz) gap
                triangles.Add(GetOrAddVertex((vx,     vz)));
                triangles.Add(GetOrAddVertex((vx + 2, vz)));
                triangles.Add(GetOrAddVertex((vx,     vz - 2)));
                // NW notch — covers the (cx, cz+1) gap
                triangles.Add(GetOrAddVertex((vx,     vz)));
                triangles.Add(GetOrAddVertex((vx - 2, vz)));
                triangles.Add(GetOrAddVertex((vx,     vz + 2)));
            }

            // SE diagonal: shared corner is BR of (cx,cz) = (cx*2+2, cz*2)
            if ( filledCells.Contains((cx + 1, cz - 1)) &&
                !filledCells.Contains((cx + 1, cz)) &&
                !filledCells.Contains((cx,     cz - 1)))
            {
                int vx = cx * 2 + 2, vz = cz * 2;
                // NE notch — covers the (cx+1, cz) gap
                triangles.Add(GetOrAddVertex((vx,     vz)));
                triangles.Add(GetOrAddVertex((vx,     vz + 2)));
                triangles.Add(GetOrAddVertex((vx + 2, vz)));
                // SW notch — covers the (cx, cz-1) gap
                triangles.Add(GetOrAddVertex((vx,     vz)));
                triangles.Add(GetOrAddVertex((vx,     vz - 2)));
                triangles.Add(GetOrAddVertex((vx - 2, vz)));
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices  = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv        = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        riverObject.AddComponent<MeshCollider>();
        riverObject.layer = LayerMask.NameToLayer("Obstacle");

        // One obstacle per path step; height follows the terrain slope.
        float obstacleSize = riverWidth * 1.5f;
        for (int pi = 0; pi < riverPathCenters.Count; pi++)
        {
            Vector3 mp = pathMeshPos[pi];
            GameObject obs = new GameObject("RiverObstacle");
            obs.transform.parent = riverObject.transform;
            obs.transform.position = new Vector3(mp.x, mp.y + riverYOffset, mp.z);
            obs.layer = LayerMask.NameToLayer("Obstacle");
            NavMeshObstacle navObs = obs.AddComponent<NavMeshObstacle>();
            navObs.shape = NavMeshObstacleShape.Box;
            navObs.carving = true;
            navObs.size = new Vector3(obstacleSize, 2f, obstacleSize);
        }

        if (enableDebugLogging)
            Debug.Log($"[RIVER] Blob mesh: {filledCells.Count} cells, {vertices.Count} vertices, {triangles.Count / 3} triangles");

        return riverObject;
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