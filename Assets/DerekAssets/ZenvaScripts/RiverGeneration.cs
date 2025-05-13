using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    //[SerializeField]
    //private Color riverColor;

    [SerializeField]
    private float waterHeight = 2f;

    private float averageHeight;

    [SerializeField]
    public GameObject gizmoPlane; // Add a variable for the river plane

    (Vector2, Vector2, Vector2, Vector2) updatedTuple = (Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
    private Vector2 skipDirection = new Vector2(0, 0);
    private float skipDirectionx = 0;
    private float skipDirectiony = 0;

    private HeightMap[,] riverchunksData;

    private Vector2 previousRiverPoint = Vector2.zero;
    int levelWidth;
    int levelDepth;

    public void GenerateRivers(int tilesWidth, int verticesWidth, TerrainData terrainData)
    {
        this.levelDepth = tilesWidth;
        this.levelWidth = verticesWidth;
        for (int riverIndex = 0; riverIndex < numberOfRivers; riverIndex++)
        {
            // populate the data to be grabbed by other methods
            riverchunksData = terrainData.chunksData;
            // choose a origin for the river
            (Vector2, Vector2, Vector2, Vector2) riverOrigin = ChooseRiverOrigin(tilesWidth, verticesWidth, terrainData);
            // build the river starting from the origin
            averageHeight = GetAverageHeight(riverOrigin, terrainData);
            BuildRiver(riverOrigin, verticesWidth, terrainData);
        }
    }

    private (Vector2, Vector2, Vector2, Vector2) ChooseRiverOrigin(int tilesWidth, int verticesWidth, TerrainData terrainData)
    {
        bool found = false;
        int randomZIndex = 150;
        int randomXIndex = 50;
        TileCoordinate tileCoordinate = terrainData.ConvertToTileCoordinate(randomXIndex, randomZIndex);
        // iterates until finding a good river origin
        while (!found)
        {
            // pick a random coordinate inside the level
            randomXIndex = Random.Range(0, tilesWidth * verticesWidth - 8);
            randomZIndex = Random.Range(0, tilesWidth * verticesWidth - 8);

            // convert from Level Coordinate System to Tile Coordinate System and retrieve the corresponding TileData
            tileCoordinate = terrainData.ConvertToTileCoordinate(randomXIndex, randomZIndex);
            HeightMap riverheightMap = terrainData.chunksData[tileCoordinate.tileXIndex, tileCoordinate.tileZIndex];

            // if the height value of this coordinate is higher than the threshold, choose it as the river origin
            float heightValue = riverheightMap.heightvalues[tileCoordinate.coordinateXIndex, tileCoordinate.coordinateZIndex];
            if (heightValue >= this.heightThreshold)
            {
                found = true;
            }

        }

        Vector2 firstOrigin = new Vector2(randomXIndex, randomZIndex);
        Vector2 secondOrigin = new Vector2(randomXIndex+1, randomZIndex);
        Vector2 thirdOrigin = new Vector2(randomXIndex, randomZIndex + 1);
        Vector2 fourthOrigin = new Vector2(randomXIndex+1, randomZIndex + 1);


        return (firstOrigin, secondOrigin, thirdOrigin, fourthOrigin);
    }


    private void BuildRiver((Vector2, Vector2, Vector2, Vector2) riverOrigin, int verticesWidth, TerrainData terrainData)
    {
        List<GameObject> riverObjs = new List<GameObject>();

        bool foundWater = false;
        int loopcount = 0;
        HashSet<Vector2> visitedCoordinates = new HashSet<Vector2>();
        List<Vector2> currentRiverList = new List<Vector2> { riverOrigin.Item1, riverOrigin.Item2, riverOrigin.Item3, riverOrigin.Item4 };
        visitedCoordinates.UnionWith(currentRiverList);
        (Vector2 firstOrigin, Vector2 secondOrigin, Vector2 thirdOrigin, Vector2 fourthOrigin) currentCoordinate = riverOrigin;

        while (!foundWater)
        {
            // Pick neighbor coordinates, if they exist, only from the grids above, below, to the left, and to the right of the currentRiverList grid
            List<(Vector2, Vector2, Vector2, Vector2)> neighboringTuples = new List<(Vector2, Vector2, Vector2, Vector2)>();
            (Vector2, Vector2, Vector2, Vector2) aboveTuple = (currentCoordinate.thirdOrigin, currentCoordinate.fourthOrigin, currentCoordinate.thirdOrigin + Vector2.up, currentCoordinate.fourthOrigin + Vector2.up);
            (Vector2, Vector2, Vector2, Vector2) belowTuple = (currentCoordinate.firstOrigin + Vector2.down, currentCoordinate.secondOrigin + Vector2.down, currentCoordinate.firstOrigin, currentCoordinate.secondOrigin);
            (Vector2, Vector2, Vector2, Vector2) leftTuple = (currentCoordinate.firstOrigin + Vector2.left, currentCoordinate.firstOrigin, currentCoordinate.thirdOrigin + Vector2.left, currentCoordinate.thirdOrigin);
            (Vector2, Vector2, Vector2, Vector2) rightTuple = (currentCoordinate.secondOrigin, currentCoordinate.secondOrigin + Vector2.right, currentCoordinate.fourthOrigin, currentCoordinate.fourthOrigin + Vector2.right);

            if (!visitedCoordinates.Contains(aboveTuple.Item3) && !visitedCoordinates.Contains(aboveTuple.Item4) && IsValidCoordinate(aboveTuple.Item3, verticesWidth, terrainData) && IsValidCoordinate(aboveTuple.Item4, verticesWidth, terrainData))
            {
                neighboringTuples.Add(aboveTuple);
            }
            if (!visitedCoordinates.Contains(leftTuple.Item1) && !visitedCoordinates.Contains(leftTuple.Item3) && IsValidCoordinate(leftTuple.Item1, verticesWidth, terrainData) && IsValidCoordinate(leftTuple.Item3, verticesWidth, terrainData))
            {
                neighboringTuples.Add(leftTuple);;
            }
            if (!visitedCoordinates.Contains(rightTuple.Item2) && !visitedCoordinates.Contains(rightTuple.Item4) && IsValidCoordinate(rightTuple.Item2, verticesWidth, terrainData) && IsValidCoordinate(rightTuple.Item4, verticesWidth, terrainData))
            {
                neighboringTuples.Add(rightTuple);
            }
            if (!visitedCoordinates.Contains(belowTuple.Item1) && !visitedCoordinates.Contains(belowTuple.Item2) && IsValidCoordinate(belowTuple.Item1, verticesWidth, terrainData) && IsValidCoordinate(belowTuple.Item2, verticesWidth, terrainData))
            {
                neighboringTuples.Add(belowTuple);
            }


            // Find the minimum neighbor tuple that has not been visited yet and flow to it
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


                var (first, second, third, fourth) = currentCoordinate;

                // Create a new tuple with updated values
                updatedTuple = (
                    first + skipDirection,
                    second + skipDirection,
                    third + skipDirection,
                    fourth + skipDirection
                );

            if (IsSkippedTuple(currentCoordinate))
            {
                minNeighborTuple = updatedTuple;
            }
            skipDirection.x = minNeighborTuple.Item1.x - currentCoordinate.firstOrigin.x ;
            skipDirection.y = minNeighborTuple.Item1.y - currentCoordinate.firstOrigin.y;

            // If we hit loop limit, we stop
            if (loopcount >= looplimit)
                {
                    //Debug.Log("Loop limit reached: " + loopcount);
                    hasNeighbor = false;
                } 
            // Check if the average height is below the water threshold
            if (averageHeight <= waterHeight)
            {
                // Stop if water is found
                hasNeighbor = false;
            }
            if (hasNeighbor)
            {
                //make river object

                    TileCoordinate riverObjectCoord = terrainData.ConvertToTileCoordinate((int)minNeighborTuple.Item1.x, (int)minNeighborTuple.Item1.y);
                    HeightMap heightMap = riverchunksData[riverObjectCoord.tileXIndex, riverObjectCoord.tileZIndex];
                    // Instantiate a 3D plane at the river point
                    Vector3 planePosition = new Vector3((int)minNeighborTuple.Item1.x, (float)(heightMap.heightvalues[riverObjectCoord.coordinateXIndex, riverObjectCoord.coordinateZIndex] - 0.4), (int)minNeighborTuple.Item1.y);
                    GameObject riverPlane = Instantiate(gizmoPlane, planePosition, Quaternion.identity);
                    riverPlane.transform.parent = riverList.transform;
                    
                    // add the river object instance to the list
                    riverObjs.Add(riverPlane);

                    // Calculate the slope between the current point and the previous point
                    if (previousRiverPoint != Vector2.zero)
                    {
                        Vector3 direction = new Vector3((int)minNeighborTuple.Item1.x - previousRiverPoint.x, minAverageHeight - averageHeight, (int)minNeighborTuple.Item1.y - previousRiverPoint.y);
                        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                        riverPlane.transform.rotation = rotation;
                    }

                    // Set the current point as the previous point for the next iteration
                    previousRiverPoint = minNeighborTuple.Item1;


                // Flow to the lowest neighbor tuple
                currentCoordinate = minNeighborTuple;
                averageHeight = minAverageHeight;
                currentRiverList = new List<Vector2> { minNeighborTuple.Item1, minNeighborTuple.Item2, minNeighborTuple.Item3, minNeighborTuple.Item4 };
                visitedCoordinates.UnionWith(currentRiverList);
                loopcount++;
            }
            else
            {
                // Depress every point in the visitedCoordinates list
                foreach (Vector2 visitedCoord in visitedCoordinates)
                {
                    //get coordinate information then depress the riverbed
                    TileCoordinate visitedTileCoord = terrainData.ConvertToTileCoordinate((int)visitedCoord.x, (int)visitedCoord.y);
                    HeightMap heightMap = riverchunksData[visitedTileCoord.tileXIndex, visitedTileCoord.tileZIndex];
                    terrainData.chunksData[visitedTileCoord.tileXIndex, visitedTileCoord.tileZIndex].heightvalues[visitedTileCoord.coordinateXIndex, visitedTileCoord.coordinateZIndex]
                        = heightMap.heightvalues[visitedTileCoord.coordinateXIndex, visitedTileCoord.coordinateZIndex] - depressAmount;
                }

                break; // Break out of the loop if there are no neighbors
            }
        }

        MergeMeshes(riverObjs, gameObject);
    }
    private float GetAverageHeight((Vector2, Vector2, Vector2, Vector2) currentCoordinate, TerrainData terrainData)
    {
        float totalHeight = 0f;
        int count = 0;
        foreach (Vector2 coord in new List<Vector2> { currentCoordinate.Item1, currentCoordinate.Item2, currentCoordinate.Item3, currentCoordinate.Item4 })
        {
            TileCoordinate tileCoord = terrainData.ConvertToTileCoordinate((int)coord.x, (int)coord.y);
            {
                HeightMap neighborheightMap = riverchunksData[tileCoord.tileXIndex, tileCoord.tileZIndex];
                totalHeight += neighborheightMap.heightvalues[tileCoord.coordinateXIndex, tileCoord.coordinateZIndex];
                count++;
            }
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
        //Debug.Log($"TupleX: {coordinate.x}, TupleZ: {coordinate.y}, X: {tileCoord.tileXIndex}, Z: {tileCoord.tileZIndex}, CoordX: {tileCoord.coordinateXIndex}, CoordZ: {tileCoord.coordinateZIndex}");
        //print(" depth = " + (terrainData.levelDepthInTiles - 1));
        return tileCoord.tileXIndex >= 0 && tileCoord.tileXIndex <= (terrainData.levelDepthInTiles-1) &&
               tileCoord.tileZIndex >= 0 && tileCoord.tileZIndex <= (terrainData.levelDepthInTiles-1) &&
               tileCoord.coordinateXIndex >= 0 && tileCoord.coordinateXIndex <= (verticesWidth) &&
               tileCoord.coordinateZIndex >= 0 && tileCoord.coordinateZIndex <= (verticesWidth);
    }

    private void MergeMeshes(List<GameObject> rivers, GameObject root){

        var meshFilters = new List<MeshFilter>();
        foreach (var river in rivers)
        {
            meshFilters.Add(river.GetComponent<MeshFilter>());
        }

        CombineInstance[] combine = new CombineInstance[meshFilters.Count];

        int i = 0;
        while (i < meshFilters.Count)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            // meshFilters[i].gameObject.SetActive(false);
            Destroy(meshFilters[i].gameObject);

            i++;
        }

        GameObject obj = new GameObject("RiverRoot");
        obj.transform.position = new Vector3(0, 0, 0);
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();

        obj.GetComponent<MeshFilter>().mesh = new Mesh();
        obj.GetComponent<MeshFilter>().mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        obj.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);

        Material sharedMat = rivers[0].GetComponent<MeshRenderer>().sharedMaterial;
        obj.GetComponent<MeshRenderer>().material = sharedMat;

        obj.gameObject.SetActive(true);

        obj.gameObject.transform.SetParent(riverList.transform, true);
    }
}
