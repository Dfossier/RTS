using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine.SceneManagement;
using RTSEngine.Utilities;

public class TerrainGenerator : MonoBehaviour
{
    public bool startGameAfterTerrainGen = true;
    [SerializeField]
    public GameObject rtsEngine;
    [SerializeField]
    public Transform sceneTransform;

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    public MeshSettings meshSettings;

    [SerializeField]
    private int levelWidthInTiles, levelDepthInTiles;

    [SerializeField]
    private TreeGeneration treeGeneration;

    [SerializeField]
    private RiverGeneration riverGeneration;

    [SerializeField]
    private StoneGeneration stoneGeneration;

    [SerializeField]
    private StoneGeneration copperoreGeneration;

    [SerializeField]
    private StoneGeneration tinoreGeneration;

    [SerializeField]
    private StoneGeneration wheatGeneration;

    [SerializeField]
    private FreeUnitGeneration freeunitGen;

    [SerializeField]
    private GameObject seaPlane;

    [SerializeField]
    private GameObject seaPlane2;

    private TerrainData terrainData;
    public HeightMapSettings heightMapSettings;
    public static HeightMapSettings Static_HeightMapSettings;
    public TextureData textureSettings;

    public Transform viewer;
    public Material mapMaterial;

    [HideInInspector]
    public int tileDepthInVertices;
    [HideInInspector]
    public int tileWidthInVertices;

    Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    float meshWorldSize;
    int chunksVisibleInViewDst;

    [HideInInspector]
    public int chunkCount = 0;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    public Unity.AI.Navigation.NavMeshSurface navMeshSurface;

    private void Awake()
    {
        Static_HeightMapSettings = heightMapSettings;
        
        if(navMeshSurface == null)
            navMeshSurface = gameObject.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
    }

    void Start()
    {
        Random.InitState(heightMapSettings.noiseSettings.seed * 3 / 2);
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);
        

        // calculate the number of vertices of the tile in each axis using its mesh
        //Vector3[] tileMeshVertices = tileSize;
        tileDepthInVertices = meshSettings.numVertsPerLine;
        tileWidthInVertices = tileDepthInVertices;

        // build an empty LevelData object, to be filled with the tiles to be generated
        terrainData = new TerrainData(tileDepthInVertices, tileWidthInVertices, this.levelDepthInTiles, this.levelWidthInTiles);
        UpdateVisibleChunks();
    }

    void Update()
    { 
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if (viewerPosition != viewerPositionOld)
        {
            foreach (TerrainChunk chunk in visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
                //this shouldn't run on fixed frame of reference
                Debug.Log("We are updating the collision Mesh");
            }

        }

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }

        if (chunkCount == levelDepthInTiles * levelWidthInTiles)
        {
            //After chunks are created, build the river, then send resulting heightmaps to OnHeightMapReceived function inside each TerrainChunk
            //this is where the chunks are all finally loaded so we will now populate the biome mesh with rivers, resources, and starting positions
            riverGeneration.GenerateRivers(this.levelWidthInTiles, tileWidthInVertices, this.terrainData);
            UpdateVisibleChunks();
            treeGeneration.GenerateTrees(this.levelWidthInTiles, this.tileWidthInVertices, this.terrainData);
            chunkCount = 0;
            Vector3 seaPlane2Position = new Vector3(123, (float)1.4, 123);
            var seaPlaneInst = Instantiate(seaPlane2, seaPlane2Position, Quaternion.identity);
            seaPlaneInst.transform.parent = transform.parent;
        }

        if (terrainData.loadCount == levelDepthInTiles*levelWidthInTiles)
        {
            
            //Rtsengine.SetActive(true);
            //freeunitGen.GenerateUnits(this.levelWidthInTiles, this.tileWidthInVertices, this.terrainData);
            Destroy(seaPlane);
            terrainData.loadCount = 0;
            StartCoroutine(InstantiateRTSEngineAfterDelay());
        }
    }
    IEnumerator InstantiateRTSEngineAfterDelay()
    {
        // Wait for the NavMesh to be built or a specific delay
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("navmesh baked!");

            GameObject.Find("debugRandomFactionSpawnpoint").GetComponent<RandomFactionSpawnpoint>().DefineFactionsStartingpoint();

            // generate resources that depends on the navmesh baked
            stoneGeneration.GenerateStones();
            copperoreGeneration.GenerateStones();
            tinoreGeneration.GenerateStones();
            wheatGeneration.GenerateStones();
            // GameObject.Find("sceneLoader").GetComponent<DerekTerrainManager>().InitializeDerekTerrain();
            if (startGameAfterTerrainGen)
            {
                SceneManager.LoadScene("GameScene");
            }
        }
        else
        {
            Debug.Log("navmesh surface is empty");
        }
        
        yield return new WaitForSeconds(20f); // Adjust the wait time as needed
        

    //    Instantiate(rtsEngine, sceneTransform);
    }
    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        for (int yOffset = 0; yOffset <= levelDepthInTiles-1; yOffset++)
        {
            for (int xOffset = 0; xOffset <= levelWidthInTiles-1; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {  

                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].OnHeightMapReceived(terrainData.chunksData[(int)xOffset, (int)yOffset]);
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                        //terrainChunkDictionary[viewedChunkCoord].UpdateCollisionMesh();
                    }

                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial, terrainData);
                        
                        terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        //this line is not in the original download, unknown origin
                        //newChunk.OnHeightMapCompleted = (map) => { textureSettings.ApplyToMaterial(mapMaterial, map); };
                        newChunk.Load();
                        newChunk.OnTerrainUpdated += UpdateNavMesh;
                        chunkCount++;
                    }
                }
             }
        }
    }

    private void OnDestroy()
    {
        //Destroy the Update Navmesh event shouldn't happen with the current code but in case it changes later no leak will be created;
        foreach(var terrainChunk in terrainChunkDictionary)
        {
            terrainChunkDictionary[terrainChunk.Key].OnTerrainUpdated -= UpdateNavMesh;
        }
    }

    void UpdateNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }

    void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
        {
            visibleTerrainChunks.Add(chunk);
        }
        else
        {
            visibleTerrainChunks.Remove(chunk);
        }
    }
}

public class TerrainData
{
    private int tileDepthInVertices, tileWidthInVertices;
    public int levelDepthInTiles;

    public HeightMap[,] chunksData;
    public int loadCount;

    public TerrainData(int tileDepthInVertices, int tileWidthInVertices, int levelDepthInTiles, int levelWidthInTiles)
    {
        // build the tilesData matrix based on the level depth and width
        chunksData = new HeightMap[levelWidthInTiles, levelDepthInTiles];

        this.tileDepthInVertices = tileDepthInVertices;
        this.tileWidthInVertices = tileWidthInVertices;
        this.levelDepthInTiles = levelDepthInTiles;
    }

    public void AddMapData(HeightMap mapData, int tileXIndex, int tileZIndex)
    {
        // save the TileData in the corresponding coordinate
        chunksData[tileXIndex, tileZIndex] = mapData;
    }

    public TileCoordinate ConvertToTileCoordinate(int xIndex, int zIndex)

    {
        // the tile index is calculated by dividing the index by the number of tiles in that axis
        int tileXIndex = (int)Mathf.Floor((float)xIndex / ((float)this.tileWidthInVertices));
        int tileZIndex = (int)Mathf.Floor((float)zIndex / ((float)this.tileDepthInVertices));

        // Adjust the Z coordinate to match the negative direction in Unity's coordinate system
        int coordinateXIndex = xIndex % this.tileWidthInVertices;
        int coordinateZIndex = this.tileDepthInVertices - (zIndex % this.tileDepthInVertices) - 1;

        TileCoordinate tileCoordinate = new TileCoordinate(tileXIndex, tileZIndex, coordinateXIndex, coordinateZIndex);
        return tileCoordinate;
    }
}
// class to represent a coordinate in the Tile Coordinate System
public class TileCoordinate
{
    public int tileXIndex;
    public int tileZIndex;
    public int coordinateXIndex;
    public int coordinateZIndex;

    public TileCoordinate(int tileXIndex, int tileZIndex, int coordinateXIndex, int coordinateZIndex)
    {
        this.tileXIndex = tileXIndex;
        this.tileZIndex = tileZIndex;
        this.coordinateXIndex = coordinateXIndex;
        this.coordinateZIndex = coordinateZIndex;
    }
}


[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float visibleDstThreshold;

    public float sqrVisibleDstThreshold
    {
        get
        {
            return visibleDstThreshold * visibleDstThreshold;
        }
    }
}