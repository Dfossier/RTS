using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeGeneration : MonoBehaviour
{
    //[SerializeField]
    //private Object trackingSphere;

    [SerializeField]
    public GameObject[] treeList;

    public int treeCount = 0;

    [SerializeField]
    private int maxTrees;

    [SerializeField]
    private int minorTreeRadius;

    [SerializeField]
    private float denseMoisture;

    private int biomeIndex = 1;

    [SerializeField]
    private GameObject[] treePrefab;

    [SerializeField]
    private float waterHeight = 2f;

    //[SerializeField]
    //private float treeWater = 0.25f;

    [SerializeField]
    private float treeHeat = .2f;

    private float neighborRadius;

   //tree generation is working pretty well, some trees generate off board, the system is still funky.  The debug is very helpful in looking at funky parts... check for x and z values and try to narrow down the relationship and where it comes from (noise generation, tile center)

    public void GenerateTrees(int tilesWidth, int verticesWidth, TerrainData terrainData)
    {

        // Create an array to store height maps
        HeightMap[,] heightMaps = new HeightMap[tilesWidth, tilesWidth];

        // Populate the array with height maps
        for (int x = 0; x < tilesWidth; x++)
        {
            for (int z = 0; z < tilesWidth; z++)
            {
                heightMaps[x, z] = terrainData.chunksData[x, z];
            }
        }


        for (int xIndex = 0; xIndex < tilesWidth * (verticesWidth - 2); xIndex++)
        {
            //currently zIndex = 0 is the equator, not the bottom of the map, which is why tree overflows.
            for (int zIndex = 1; zIndex < tilesWidth * (verticesWidth - 2); zIndex++)
            {
                if (treeCount > maxTrees) { break; }
                
                // convert from Level Coordinate System to Tile Coordinate System and retrieve the corresponding TileData
                TileCoordinate tileCoordinate = terrainData.ConvertToTileCoordinate(xIndex, zIndex);
                //print("verticesWidth = " + verticesWidth + " Xindex: " + xIndex + " Zindex: " + zIndex + " tileCordinate.coordinateXIndex: " + tileCoordinate.coordinateXIndex + " tileCordinate.tileXIndex: " + tileCoordinate.tileXIndex + " tileCordinate.coordinateZIndex: " + tileCoordinate.coordinateZIndex + " tileCordinate.tileZIndex: " + tileCoordinate.tileZIndex);
                
                HeightMap treeHeightMap = heightMaps[tileCoordinate.tileXIndex, tileCoordinate.tileZIndex];

                neighborRadius = minorTreeRadius;
                float maxValue = 0f;
                // this calculation is linked to the TileCoordinate class and how that is calculated... changed from (verticesWidth - tileCoordinate.coordinateZIndex-1)
                float treeValue = treeHeightMap.moisture[(tileCoordinate.coordinateXIndex), (tileCoordinate.coordinateZIndex)];


                if (treeValue >= denseMoisture)
                {
                    neighborRadius = 0;
                }


                // compares the current tree noise value to the neighbor ones using loops to check all values.. this should be done using only X and Z index, then convert to coordinate to check values
                int neighborXBegin = (int)Mathf.Max(0, xIndex - neighborRadius);
                int neighborXEnd = (int)Mathf.Min(tilesWidth * verticesWidth - 2 * tilesWidth, xIndex + neighborRadius);
                int neighborZBegin = (int)Mathf.Max(0, zIndex - neighborRadius);
                int neighborZEnd = (int)Mathf.Min(tilesWidth * verticesWidth - 2 * tilesWidth, zIndex + neighborRadius);

                for (int neighborZ = neighborZBegin; neighborZ <= neighborZEnd; neighborZ++)
                {
                    for (int neighborX = neighborXBegin; neighborX <= neighborXEnd; neighborX++)
                    {
                        //print("neighbor X: " + neighborX + " neighborZ: " + neighborZ);
                        
                        //convert neighborX and neighborZ into coordinates in order to pull the right value from the right tile's map
                        TileCoordinate neighborCoordinate = terrainData.ConvertToTileCoordinate(neighborX, neighborZ);
                        //print("coordinateX: " + neighborCoordinate.coordinateXIndex + " coordinateZ: " + neighborCoordinate.coordinateZIndex);
                        //this needs to be the tileIndex, don't change it
                        HeightMap neighborHeightMap = terrainData.chunksData[neighborCoordinate.tileXIndex, neighborCoordinate.tileZIndex];
                        
                        float neighborValue = neighborHeightMap.moisture[(neighborCoordinate.coordinateXIndex), (neighborCoordinate.coordinateZIndex)];

                        // save the maximum tree noise value in the radius
                        if (neighborValue - .05 >= maxValue)
                        {
                            maxValue = neighborValue;
                        }
                        //Debug.Log("neighborValue = " + neighborValue);
                    }
                }

                //if the current tree noise value is the maximum one, place a tree in this location
                if (treeValue >= maxValue)
                {
                    //we are accessing here information from the heightmaps in order to build trees appropriate to biomes
                    //transforms are required for tree position due to meshgenerator method building negative in the z direction
                    if (treeHeightMap.heightvalues[(tileCoordinate.coordinateXIndex), (tileCoordinate.coordinateZIndex)] >= waterHeight
                    && treeHeightMap.heat[(tileCoordinate.coordinateXIndex), (tileCoordinate.coordinateZIndex)] <= treeHeat)
                    {
                        //because of meshgenerator line 47, the vertices are construction from top left to the right and then down, it means our tile coordinate system starts the same way
                        // 0,0 is the top left of the first tile.  The tiles then build to the right and up.  So x should be straight forward.  However, I believe we have to shift all -1 in the X plane (I think because of the normals calculation)
                        Vector3 treePosition = new Vector3(xIndex-1-(tileCoordinate.tileXIndex*3),
                            treeHeightMap.heightvalues[tileCoordinate.coordinateXIndex,
                            tileCoordinate.coordinateZIndex],
                            //(tileCoordinate.tileZIndex + 1): This part accounts for the tile index in the Z direction.If your tile system starts at 0 and increases positively, then this adjustment is correct.
                            //and for Z we need to adjust for the start position at the top of tile, so we add the tileIndex [from 0 to 1] multiplied by the verticesWidth then subtract the zIndex since the mesh builds downward. 
                            //the weird constants are from the mesh generation, I believe because each tile generates an extra 3 layers of unused vertices.  The first has to correct for the extra vertices, then add 1.  reasons.
                            (tileCoordinate.tileZIndex + 1) * (verticesWidth - 3) - (tileCoordinate.coordinateZIndex-4));

                        //if (treeHeightMap.moisture[(verticesWidth - 1 - tileCoordinate.coordinateXIndex), (verticesWidth - 1 - tileCoordinate.coordinateZIndex)] >= .5)
                        {
                            biomeIndex = 0;
                        }
                        //else
                        //{
                        //    biomeIndex = 1;
                        //}

                        GameObject tree = Instantiate(this.treePrefab[biomeIndex], treePosition, Quaternion.identity) as GameObject;
                        tree.transform.parent = treeList[biomeIndex].transform;

                        //Debug.Log("verticesWidth " + (verticesWidth) + " zIndex " + zIndex + " tileCoordinate.tileZIndex" + tileCoordinate.tileZIndex + " Tree Z value: " + ((-zIndex - (verticesWidth - tilesWidth) / 2) - (1 * (tileCoordinate.tileZIndex) * verticesWidth)));
                        //Debug.Log("X: " + xIndex + " Z: " + zIndex + " water: " + treeHeightMap.moisture[(verticesWidth - 1 - tileCoordinate.coordinateXIndex), (verticesWidth - 1 - tileCoordinate.coordinateZIndex)]);

                        //use this to make object bigger or smaller
                        //tree.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                        treeCount++;
                    }
                }
            }
        }
    }
}
