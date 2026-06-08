using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine.SceneManagement;
using RTSEngine.Utilities;
using RTSEngine.Entities;
using RTSEngine.ResourceExtension;

public class TerrainGenerator : MonoBehaviour
{
    public bool IsDebug = false;
    public bool PreDebug = false;
    public bool PostDebug = false;


    public bool startGameAfterTerrainGen = true;
    [SerializeField]
    public GameObject rtsEngine;
    public GameObject RtsEngineInstance = null;
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

    // --- Public map-size accessors (used by TradeBuildingSpawner to find the map edges) ---
    // Note: meshWorldSize is computed in Start(), so these are only valid after the terrain has started.
    public float MeshWorldSize => meshWorldSize;
    public int LevelWidthInTiles => levelWidthInTiles;
    public int LevelDepthInTiles => levelDepthInTiles;
    /// <summary> Full map size in world units. X = width, Z = depth. Valid after Start(). </summary>
    public Vector3 MapWorldSize => new Vector3(levelWidthInTiles * meshWorldSize, 0f, levelDepthInTiles * meshWorldSize);

    [HideInInspector]
    public int chunkCount = 0;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    public NavMeshSurface navMeshSurface;

    public Transform ResourcesParent = null;

    /// <summary>
    /// Presorting Resources based on distance from player start positions
    /// </summary>
    /// 

    public List<GameObject> GeneratedTrees = new();
    public List<GameObject> GeneratedStone = new();
    public List<GameObject> GeneratedCopper = new();
    public List<GameObject> GeneratedTin = new();
    public List<GameObject> GeneratedWheat = new();

    public List<Resource> PreLoadResources = new();
    public List<Resource> PostLoadResources = new();

    public bool ResourcesSorted = false;

    public RandomFactionSpawnpoint RandomFactionSpawnpoint = null;

    private void Awake()
    {
        Static_HeightMapSettings = heightMapSettings;

        if (navMeshSurface == null && gameObject.TryGetComponent(out NavMeshSurface nav))
            navMeshSurface = nav;

        // Persist terrain across scene loads if RTS engine loads a different scene
        if (startGameAfterTerrainGen)
        {
            DontDestroyOnLoad(gameObject);
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reapply material properties whenever a new scene loads
        if (mapMaterial != null && textureSettings != null)
        {
            StartCoroutine(ReapplyMaterialProperties());
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;

        //Destroy the Update Navmesh event shouldn't happen with the current code but in case it changes later no leak will be created;
        foreach(var terrainChunk in terrainChunkDictionary)
        {
            terrainChunkDictionary[terrainChunk.Key].OnTerrainUpdated -= UpdateNavMesh;
        }
    }

    void OnEnable()
    {
        // Re-apply material properties when object is enabled (e.g., after scene load)
        if (mapMaterial != null && textureSettings != null)
        {
            StartCoroutine(ReapplyMaterialProperties());
        }
    }

    IEnumerator ReapplyMaterialProperties()
    {
        // Wait a frame to ensure scene is fully loaded
        yield return null;
        yield return new WaitForEndOfFrame();

        if (mapMaterial != null && textureSettings != null && heightMapSettings != null)
        {
            //Debug.Log($"Reapplying material properties. Material: {mapMaterial.name}, Shader: {mapMaterial.shader.name}");
            textureSettings.ApplyToMaterial(mapMaterial);
            textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

            // Verify all terrain chunks are using the shared material
            int chunkCount = 0;
            foreach (var kvp in terrainChunkDictionary)
            {
                chunkCount++;
            }
            Debug.Log($"Material properties reapplied to {chunkCount} terrain chunks");
        }
    }

    void Start()
    {
        Random.InitState(heightMapSettings.noiseSettings.seed * 3 / 2);
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        float maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

        if (RandomFactionSpawnpoint == null && GameObject.Find("debugRandomFactionSpawnpoint").TryGetComponent(out RandomFactionSpawnpoint factionSpawnpoints))
            RandomFactionSpawnpoint = factionSpawnpoints;

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
                if(IsDebug)
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
            if(treeGeneration != null)
            {
                treeGeneration.SetTerrainGenerator(this);
                treeGeneration.GenerateTrees(this.levelWidthInTiles, this.tileWidthInVertices, this.terrainData);
            }
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

            // Re-apply material properties after all chunks are loaded to prevent black tiles
            textureSettings.ApplyToMaterial(mapMaterial);
            textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

            terrainData.loadCount = 0;
            StartCoroutine(InstantiateRTSEngineAfterDelay());
        }
    }
    IEnumerator InstantiateRTSEngineAfterDelay()
    {
        // Wait for the NavMesh to be built or a specific delay
        if (navMeshSurface != null)
        {

            if(stoneGeneration != null)
                stoneGeneration.SetGenerator(this);
            if(copperoreGeneration != null)
                copperoreGeneration.SetGenerator(this);
            if(tinoreGeneration != null)
                tinoreGeneration.SetGenerator(this);
            if(wheatGeneration != null)
                wheatGeneration.SetGenerator(this);

            navMeshSurface.BuildNavMesh();
            // BuildFilteredNavMesh(transform, 10f);
            if (IsDebug)
                Debug.Log($"navmesh baked!");

            RandomFactionSpawnpoint.DefineFactionsStartingpoint();

            // generate resources that depends on the navmesh baked
            stoneGeneration.GenerateStones();
            copperoreGeneration.GenerateStones();
            tinoreGeneration.GenerateStones();
            wheatGeneration.GenerateStones();

            List<Vector3> spawns = new();
            if(RandomFactionSpawnpoint.PlayerSpawnpoint != Vector3.zero)
            {
                spawns.Add(RandomFactionSpawnpoint.PlayerSpawnpoint);
            }
            if(RandomFactionSpawnpoint.NPCsSpawnpoint.Length > 0)
            {
                foreach(var npc in RandomFactionSpawnpoint.NPCsSpawnpoint)
                {
                    spawns.Add(npc);
                }
            }
            if(spawns.Count > 0)
            {
                if (IsDebug)
                    Debug.Log($"Counted {spawns.Count} total start positions");
            }

            if(GeneratedTrees.Count > 0)
            {
                foreach( var tree in GeneratedTrees)
                {
                    if (tree != null && spawns.Count > 0)
                    {
                        for(int i = 0; i < spawns.Count; i++)
                        {
                            float distanceFromSpawn = Vector3.Distance(spawns[i],tree.transform.position);
                            if(distanceFromSpawn < 55)
                            {
                                if (IsDebug && PreDebug)
                                    Debug.Log($"Tree pre loading: {tree.name}");
                                if (tree.TryGetComponent(out Resource rss))
                                {
                                    if(!PreLoadResources.Contains(rss))
                                        PreLoadResources.Add(rss);
                                }
                            }
                            else
                            {
                                if (IsDebug && PostDebug)
                                    Debug.Log($"Tree post loading: {tree.name}");
                                if (tree.TryGetComponent(out Resource rss))
                                {
                                    if (!PostLoadResources.Contains(rss))
                                        PostLoadResources.Add(rss);
                                }
                            }
                        }
                    }
                }
            }
            if (GeneratedStone.Count > 0)
            {
                foreach (var stone in GeneratedStone)
                {
                    if(stone != null && spawns.Count > 0)
                    {
                        for (int i = 0; i < spawns.Count; i++)
                        {
                            float distanceFromSpawn = Vector3.Distance(spawns[i], stone.transform.position);
                            if (distanceFromSpawn < 55)
                            {
                                if (IsDebug && PreDebug)
                                    Debug.Log($"Stone pre loading: {stone.name}");
                                if (stone.TryGetComponent(out Resource rss))
                                {
                                    PreLoadResources.Add(rss);
                                }
                            }
                            else
                            {
                                if (IsDebug && PostDebug)
                                    Debug.Log($"Stone post loading: {stone.name}");
                                if (stone.TryGetComponent(out Resource rss))
                                {
                                    PostLoadResources.Add(rss);
                                }
                            }
                        }
                    }
                }
            }
            if (GeneratedCopper.Count > 0)
            {
                foreach (var copper in GeneratedCopper)
                {
                    if (copper != null && spawns.Count > 0)
                    {
                        for (int i = 0; i < spawns.Count; i++)
                        {
                            float distanceFromSpawn = Vector3.Distance(spawns[i], copper.transform.position);
                            if (distanceFromSpawn < 55)
                            {
                                if (IsDebug && PreDebug)
                                    Debug.Log($"Copper pre loading: {copper.name}");
                                if (copper.TryGetComponent(out Resource rss))
                                {
                                    PreLoadResources.Add(rss);
                                }
                            }
                            else
                            {
                                if (IsDebug && PostDebug)
                                    Debug.Log($"Copper post loading: {copper.name}");
                                if (copper.TryGetComponent(out Resource rss))
                                {
                                    PostLoadResources.Add(rss);
                                }
                            }
                        }
                    }
                }
            }
            if (GeneratedWheat.Count > 0)
            {
                foreach (var wheat in GeneratedWheat)
                {
                    if (wheat != null && spawns.Count > 0)
                    {
                        for (int i = 0; i < spawns.Count; i++)
                        {
                            float distanceFromSpawn = Vector3.Distance(spawns[i], wheat.transform.position);
                            if (distanceFromSpawn < 55)
                            {
                                if (IsDebug && PreDebug)
                                    Debug.Log($"Wheat pre loading: {wheat.name}");
                                if (wheat.TryGetComponent(out Resource rss))
                                {
                                    PreLoadResources.Add(rss);
                                }
                            }
                            else
                            {
                                if (IsDebug && PostDebug)
                                    Debug.Log($"Wheat post loading: {wheat.name}");
                                if (wheat.TryGetComponent(out Resource rss))
                                {
                                    PostLoadResources.Add(rss);
                                }
                            }
                        }
                    }
                }
            }
            if (GeneratedTin.Count > 0)
            {
                foreach (var tin in GeneratedTin)
                {
                    if (tin != null && spawns.Count > 0)
                    {
                        for (int i = 0; i < spawns.Count; i++)
                        {
                            float distanceFromSpawn = Vector3.Distance(spawns[i], tin.transform.position);
                            if (distanceFromSpawn < 55)
                            {
                                if (IsDebug && PreDebug)
                                    Debug.Log($"Tin pre loading: {tin.name}");
                                if (tin.TryGetComponent(out Resource rss))
                                {
                                    PreLoadResources.Add(rss);
                                }
                            }
                            else
                            {
                                if (IsDebug && PostDebug)
                                    Debug.Log($"Tin post loading: {tin.name}");
                                if (tin.TryGetComponent(out Resource rss))
                                {
                                    PostLoadResources.Add(rss);
                                }
                            }
                        }
                    }
                }
            }

            if (PreLoadResources.Count > 0)
            {
                foreach (var resource in PreLoadResources)
                {
                    if (resource != null)
                        resource.transform.SetParent(ResourcesParent);
                }
            }
            gameObject.GetComponent<GrassGeneration>().GenerateGrassOnNavMesh();

            // Re-apply material properties one final time before scene transition
            textureSettings.ApplyToMaterial(mapMaterial);
            textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

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
        
        yield return new WaitForSeconds(200f); // Adjust the wait time as needed
        

        //RtsEngineInstance = Instantiate(rtsEngine, sceneTransform);
        if(RtsEngineInstance != null)
        {
            ResourceManager resourceManager = FindFirstObjectByType<ResourceManager>();

            resourceManager.TGenerator = this;
            //resourceManager.StartPostLoad();
                 
        }
    }

    /* I will keep this here for future reference
    void BuildFilteredNavMesh(Transform parent, float minRegionArea, int agentTypeID = 0, LayerMask includedLayers = default, NavMeshCollectGeometry geometry = NavMeshCollectGeometry.RenderMeshes)
    {
        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(
            parent,
            includedLayers == default ? ~0 : includedLayers,
            geometry,
            0,
            new List<NavMeshBuildMarkup>(),
            sources
        );

        var settings = NavMesh.CreateSettings();
        settings.agentTypeID = agentTypeID;
        settings.minRegionArea = minRegionArea;

        Vector3 specificCenter = new Vector3(0f, 2.596704f, 0f);  // Use exact center from NavMeshSurface inspector
        var bounds = new Bounds(specificCenter, new Vector3(1600.05f, 2.596704f, 1600.05f));

        var data = NavMeshBuilder.BuildNavMeshData(
            settings,
            sources,
            bounds,
            Vector3.zero,
            Quaternion.identity
        );


        if (data != null)
        {
            NavMesh.RemoveAllNavMeshData();
            NavMesh.AddNavMeshData(data);
        }
    }
    */

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