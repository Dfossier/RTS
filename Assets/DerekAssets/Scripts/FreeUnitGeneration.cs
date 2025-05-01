using RTSEngine.UnitExtension;
using RTSEngine.Entities;
using RTSEngine.Game;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class FreeUnitGeneration : MonoBehaviour
{
    //[SerializeField]
    //private Object trackingSphere;

    public GameObject unitList;

    public int unitCount = 0;

    private int minorUnitRadius;

    [SerializeField]
    private int uBiomeIndex = 1;

    public List<GameObject> unitPrefabs = new List<GameObject>();

    [SerializeField]
    private float unitHeight = 2f;

    [SerializeField]
    private float unitWater = 0.25f;

    [SerializeField]
    private float unitHeat = .2f;

    private int unitAttempts;

    private Unit iUnitPrefab;

    private float uNeighborRadius;

    //[SerializeField]
    //public UnitManager unitMgr;

    //[SerializeField]
    //public GameManager gameMgr;

    //get game manager to register units
    //private void Awake()
    //{
        //old code not sure if we will use
        //targetAmount = amountRange.getRandomValue(); //set the target amount of units to spawn
        //spawnReload = spawnReloadRange.getRandomValue();

        //if (unitMgr == null) //if the Unit Manager component hasn't been set, get it
        //    unitMgr = FindObjectOfType(typeof(UnitManager)) as UnitManager;

        //if (gameMgr == null) //if the Unit Manager component hasn't been set, get it
        //    gameMgr = FindObjectOfType(typeof(GameManager)) as GameManager;
    //}


    public void GenerateUnits(int tilesWidth, int verticesWidth, TerrainData terrainData)
    {
        for (int xIndex = 4; xIndex < (tilesWidth * (verticesWidth) - 1); xIndex++)
        {
            for (int zIndex = 4; zIndex < (tilesWidth * (verticesWidth) - 1); zIndex++)
            {
                if (unitCount > 10) { break; }

                // convert from Level Coordinate System to Tile Coordinate System and retrieve the corresponding TileData
                TileCoordinate tileCoordinate = terrainData.ConvertToTileCoordinate(xIndex, zIndex);
                HeightMap unitHeightMap = terrainData.chunksData[tileCoordinate.tileXIndex, tileCoordinate.tileZIndex];

                uNeighborRadius = minorUnitRadius;
                float maxUnitValue = 0f;
                float unitValue = unitHeightMap.unitvalues[(verticesWidth - 1 - tileCoordinate.coordinateXIndex), (verticesWidth - 1 - tileCoordinate.coordinateZIndex)];
                //Debug.Log("xIndex: tilecoordinate.coordinateXIndex" + xIndex + "+" + tileCoordinate.coordinateXIndex + "vert width" + verticesWidth);
                //Debug.Log("zIndex: tilecoordinate.coordinateZIndex" + zIndex + "+" + tileCoordinate.coordinateZIndex);

                //if (unitValue >= .95)
                //{
                //    uNeighborRadius = 0;
                //}


                //// compares the current tree noise value to the neighbor ones
                //// this is where you calculate tree density... highest noisemap value compared to neighbors of a certain radius
                //// the problem is that you only look for neighbors once then run through all of the trees... need this to be linked together somehow
                int neighborXBegin = (int)Mathf.Max(0, tileCoordinate.coordinateXIndex - uNeighborRadius);
                int neighborXEnd = (int)Mathf.Min(verticesWidth - 1, tileCoordinate.coordinateXIndex + uNeighborRadius);
                int neighborZBegin = (int)Mathf.Max(0, tileCoordinate.coordinateZIndex - uNeighborRadius);
                int neighborZEnd = (int)Mathf.Min(verticesWidth - 1, tileCoordinate.coordinateZIndex + uNeighborRadius);

                for (int neighborZ = neighborZBegin; neighborZ <= neighborZEnd; neighborZ++)
                {
                    for (int neighborX = neighborXBegin; neighborX <= neighborXEnd; neighborX++)
                    {
                        float uNeighborValue = unitHeightMap.moisture[(verticesWidth - 1 - neighborX), (verticesWidth - 1 - neighborZ)];

                        // save the maximum tree noise value in the radius
                        if (uNeighborValue - .05 >= maxUnitValue)
                        {
                            maxUnitValue = uNeighborValue;
                        }
                        //Debug.Log("neighborValue = " + neighborValue);
                    }
                }

                //if the current tree noise value is the maximum one, place a tree in this location
                if (unitValue >= maxUnitValue)
                {
                    //the trees are drawn with a normal coordinate system with zero at bottom left, but the terrain data somehow is zero from bottom right of bottom left tile
                    //therefor what we need to do is to reassign the height values to pull from the bottom left.

                    if (unitHeightMap.heightvalues[(verticesWidth - 1 - tileCoordinate.coordinateXIndex), (verticesWidth - 1 - tileCoordinate.coordinateZIndex)] >= unitHeight &&
                unitHeightMap.unitvalues[(verticesWidth - 1 - tileCoordinate.coordinateXIndex), (verticesWidth - 1 - tileCoordinate.coordinateZIndex)] >= .98)
                    {
                        //because of meshgenerator line 47, the vertices are construction from top left to the right and then down, it means our tile coordinate system starts the same way
                        // 0,0 is the top left of the first tile.  The tiles then build to the right and up.  So x should be straight forward.  However, I believe we have to shift all -1 in the X plane (I think because of the normals calculation)
                        Vector3 unitPosition = new Vector3(xIndex - 1 - (tileCoordinate.tileXIndex * 3),
                            unitHeightMap.heightvalues[tileCoordinate.coordinateXIndex,
                            tileCoordinate.coordinateZIndex],
                            //(tileCoordinate.tileZIndex + 1): This part accounts for the tile index in the Z direction.If your tile system starts at 0 and increases positively, then this adjustment is correct.
                            //and for Z we need to adjust for the start position at the top of tile, so we add the tileIndex [from 0 to 1] multiplied by the verticesWidth then subtract the zIndex since the mesh builds downward. 
                            //the weird constants are from the mesh generation, I believe because each tile generates an extra 3 layers of unused vertices.  The first has to correct for the extra vertices, then add 1.  reasons.
                            (tileCoordinate.tileZIndex + 1) * (verticesWidth - 3) - (tileCoordinate.coordinateZIndex - 4));

                        Debug.Log("unitPrefab = " + unitPrefabs[uBiomeIndex] + " unitPosition = " + unitPosition);
                    //find a position on the navmesh
                        while (unitAttempts <= 5)
                        {
                            NavMeshHit hit;

                            if (NavMesh.SamplePosition(unitPosition, out hit, 1.0f, NavMesh.AllAreas))
                            {
                                unitPosition = hit.position;
                                break;
                            }
                            else
                            {
                                float newX = unitPosition.x + Random.value * 2;
                                float newZ = unitPosition.z + Random.value * 2;
                                Vector3 newPosition = new Vector3(newX, 5f, newZ);
                                unitPosition = newPosition;
                                unitAttempts++;
                            }
                        }



                        //if (unitHeightMap.moisture[(verticesWidth - 1 - tileCoordinate.coordinateXIndex), (verticesWidth - 1 - tileCoordinate.coordinateZIndex)] >= .75)
                        //{
                        //    uBiomeIndex = 0;
                        //}
                        //else
                        //{
                        //    uBiomeIndex = 0;
                        //}
                        //iUnitPrefab = this.unitPrefabs[uBiomeIndex].GetComponent<Unit>();
                        GameObject unitGameObject = Instantiate(unitPrefabs[uBiomeIndex], unitPosition, Quaternion.identity) as GameObject;
                        //iunitPrefab = unitPrefab[uBiomeIndex] as IUnit;
                        //unitMgr.CreateUnit(iUnitPrefab as IUnit, unitPosition, Quaternion.identity, new InitUnitParameters
                        //{
                        //    //factionID = factionSlot.ID, // Comment out and set free to true for faction-less unit
                        //    free = true,
                        //    //factionID = -1,

                        //    setInitialHealth = false,

                        //    rallypoint = null,
                        //    gotoPosition = unitPosition,
                        //});

                        //IUnit unitToAdd = unitGameObject.GetComponent<Unit>() as IUnit;
                        unitGameObject.transform.parent = unitList.transform;
                        unitCount++;
                        Debug.Log("Unit count = " + unitCount);
                        unitAttempts = 0;

                    //Debug.Log("X: " + xIndex + " Z: " + zIndex + " water: " + treeHeightMap.moisture[(verticesWidth - 1 - tileCoordinate.coordinateXIndex), (verticesWidth - 1 - tileCoordinate.coordinateZIndex)]);

                    //use this to make object bigger or smaller
                    //tree.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

                    }
                }
            }
        }
    }
}
