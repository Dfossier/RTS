using System.Collections.Generic;
using UnityEngine;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.BuildingExtension;
using RTSEngine.Cameras;
using RTSEngine.Event;
using RTSEngine.Faction;
using RTSEngine.ResourceExtension;
using RTSEngine.Selection;
using RTSEngine.UnitExtension;
using System.Linq;
using System;
using RTSEngine.Terrain;
using RTSEngine.Health;
using RTSEngine;
using System.Linq;

public class RTSEScenePrepManager : MonoBehaviour, IPreRunGameService
{
    [HideInInspector]
    public int TabID = 0;
    #region RTSE Service calls
    protected IGlobalEventPublisher GlobalEvent { private set; get; }
    protected IGameManager GameMgr { private set; get; }
    protected IBuildingManager BuildingMgr { private set; get; }
    protected IUnitManager UnitMgr { private set; get; }
    protected ISelectionManager SelectionMgr { private set; get; }
    protected IMainCameraController CamController { private set; get; }
    protected IResourceManager ResourceMgr { private set; get; }
    protected ITerrainManager TerrainMgr { private set; get; }
    #endregion

    #region Properties
    [Tooltip("Parent Transform for the Player start position markers")]
    public Transform PlayerPositionsParent = null;
    public List<PlayerStartLocation> PlayerStartLocations = new();
    public List<AvailableFaction> AvailableFactions = new();
    public GameObject TerrainsParent = null;
    public bool ShowDebug = false;
    [Tooltip("Set the prefab here for the system to add to the map.")]
    public GameObject PlayerStartPointPrefab = null;
    public FactionTypeInfo FactionTypeAdder;
    public int NewPlayerIndex = 0;
    public bool IsPlacingPlayerStart = false;
    protected int GameMgrFactionCount;
    #endregion

    public void Init(IGameManager GameMgr)
    {
        this.GameMgr = GameMgr;
        GameMgrFactionCount = this.GameMgr.FactionCount;
        this.GlobalEvent = GameMgr.GetService<IGlobalEventPublisher>();
        this.BuildingMgr = GameMgr.GetService<IBuildingManager>();
        this.UnitMgr = GameMgr.GetService<IUnitManager>();
        this.SelectionMgr = GameMgr.GetService<ISelectionManager>();
        this.ResourceMgr = GameMgr.GetService<IResourceManager>();
        this.CamController = GameMgr.GetService<IMainCameraController>();
        this.TerrainMgr = GameMgr.GetService<ITerrainManager>();

        this.GameMgr.GameStartRunning += HandleGameStartRunning;

    }

    public void HandleGameStartRunning(IGameManager source, EventArgs args)
    {
        if(ShowDebug)
            Debug.Log("Scene Prep: HandleGameStartRunning");
        if (this.AvailableFactions.Count > 0 && source.FactionCount > 0 && this.PlayerPositionsParent != null && PlayerPositionsParent.childCount >= source.FactionCount)
        {
            if (ShowDebug)
                Debug.Log("Scene Prep:  PlayerPositionsParent.childCount: " + PlayerPositionsParent.childCount + " >= source.FactionCount: " + source.FactionCount);
            Debug.Log("Scene Prep: source.ActiveFactionSlots.Count :: " + source.ActiveFactionCount);
            if (ShowDebug)
                Debug.Log("Scene Prep: About to loop throught ActiveFactionSlots :: Count is = " + source.ActiveFactionCount);
            //foreach (FactionSlot facSlot in source.ActiveFactionSlots) {
            for (int j = 0; j < source.ActiveFactionCount; j++)
            {
                FactionSlot facSlot = (FactionSlot)source.FactionSlots.ElementAt(j);
                if (ShowDebug)
                {
                    Debug.Log("----------------------------------------");
                    Debug.Log("Scene Prep: facSlot.ID :: " + facSlot.ID);
                }
                AvailableFaction thisFactionData = AvailableFactions.Find(AF => AF.FactionType == facSlot.Data.type);
                if (thisFactionData != null)
                {
                    /* if (ShowDebug)
                         Debug.Log("Scene Prep: facSlot.FactionSpawnPosition: " + facSlot.FactionSpawnPosition + " facSlotID: "+ facSlot);*/
                    if (ShowDebug)
                    {
                        Debug.Log("Scene Prep: thisFactionData != null");
                        Debug.Log("Scene Prep: About to Loop through PlayerPositionsParent Children to match SpawnPosition :: PlayerPositionsParent.childCount: " + this.PlayerPositionsParent.childCount);
                    }
                    // First we will get the position GO for the player by index
                    for (int i = 0; i < this.PlayerPositionsParent.childCount; i++) //for (int i = 0; i < GameMgrFactionCount; i++)
                    {
                        if (this.PlayerPositionsParent.GetChild(i).TryGetComponent(out PlayerStartLocation thisPlayersStart))
                        {
                            if (this.PlayerPositionsParent.GetChild(i).position == facSlot.FactionSpawnPosition)
                            {
                                if (ShowDebug)
                                    Debug.Log("Scene Prep: this.PlayerPositionsParent.GetChild(i).position == facSlot.FactionSpawnPosition at i = "+i+ ", facSlot.ID = " + facSlot.ID);
                                if (facSlot.IsLocalPlayerFaction())
                                {
                                   // this.CamController.PanningHandler.SetPosition(thisPlayersStart.BuildingsParent.GetChild(0).position);
                                    this.CamController.PanningHandler.LookAt(thisPlayersStart.BuildingsParent.GetChild(0).position, smooth: false);
                                    if (ShowDebug)
                                        Debug.Log("Scene Prep: facSlot.IsLocalPlayerFaction ");
                                }
                                // For now we can get the child objects for buildings and units by index so we first run a check on children
                                // We use index 0 for building positions
                                if (thisPlayersStart.BuildingsParent.childCount > 0)
                                {
                                    if (ShowDebug)
                                        Debug.Log("Scene Prep: thisPlayersStart.BuildingsParent.childCount > 0 :: Count = "+ thisPlayersStart.BuildingsParent.childCount);
                                    foreach (Transform child in thisPlayersStart.BuildingsParent)
                                    {
                                        if (ShowDebug)
                                            Debug.Log("Scene Prep: Looping Through thisPlayersStart.BuildingsParent ");
                                        if (child != null)
                                        {
                                            if (ShowDebug)
                                                Debug.Log("Scene Prep: Looping Through thisPlayersStart.BuildingsParent -> child != null ");
                                            // this makes sure that it only fires if there is a marker comp, standard GetComponent is not reliable as will always create an empty instance of that comp.
                                            if (child.gameObject.TryGetComponent(out FactionBuildingMarker bldngMarker))
                                            {
                                                if (ShowDebug)
                                                    Debug.Log("Scene Prep: Looping Through thisPlayersStart.BuildingsParent -> child.gameObject.TryGetComponent(out FactionBuildingMarker bldngMarker) ");
                                                if (thisFactionData.FactionBuildings.ElementAtOrDefault(bldngMarker.buildingIndexToSpawn).IsValid())
                                                {
                                                    IBuilding spawnMe = thisFactionData.FactionBuildings.ElementAtOrDefault(bldngMarker.buildingIndexToSpawn).Building;
                                                    if (ShowDebug)
                                                        Debug.Log("Scene Prep: ElementAtOrDefault(bldngMarker.buildingIndexToSpawn).Building " + spawnMe);
                                                    if (spawnMe != null)
                                                    {
                                                        if (ShowDebug)
                                                            Debug.Log("Scene Prep: About to place building -> spawnMe != null " + spawnMe.Code);

                                                        IBuilding placedBuilding = BuildingMgr.CreatePlacedBuildingLocal(
                                                            spawnMe,
                                                            child.position,
                                                            child.rotation,
                                                            new InitBuildingParameters
                                                            {
                                                                buildingCenter = spawnMe.BorderComponent,
                                                                factionID = facSlot.ID,
                                                                isBuilt = true,
                                                                setInitialHealth = true,
                                                                initialHealth = thisFactionData.FactionBuildings.ElementAtOrDefault(bldngMarker.buildingIndexToSpawn).StartingHealth,
                                                                giveInitResources = true,
                                                                playerCommand = false
                                                            }
                                                        );
                                                        if (ShowDebug)
                                                            Debug.Log("Scene Prep: Successwe have placed IBuilding -> " + placedBuilding.ToString());
                                                    }
                                                } else
                                                {
                                                    if (ShowDebug)
                                                        Debug.LogWarning("Scene Prep: No faction building set in ScenePrepManager or too many building markers for the amount of buildings to spawn" +
                                                        "Look at the PlayerStartPosition no: " + i);
                                                }
                                            }
                                        }
                                    }
                                }
                                // We do the same here for units on index 1
                                if (thisPlayersStart.UnitsParent.childCount > 0)
                                {
                                    if (ShowDebug)
                                        Debug.Log("Scene Prep: thisPlayersStart.UnitsParent.childCount > 0 :: Count = " + thisPlayersStart.UnitsParent.childCount);
                                    foreach (Transform child in thisPlayersStart.UnitsParent)
                                    {
                                        if (ShowDebug)
                                            Debug.Log("Scene Prep: Looping Through thisPlayersStart.UnitsParent ");

                                        if (child != null)
                                        {
                                            if (ShowDebug)
                                                Debug.Log("Scene Prep: Looping Through thisPlayersStart.UnitsParent -> child != null ");
                                            if (child.gameObject.TryGetComponent(out FactionUnitMarker unitMarker))
                                            {
                                                Debug.Log("Scene Prep: unitMarker = " + unitMarker.unitIndexToSpawn);
                                                if (ShowDebug)
                                                    Debug.Log("Scene Prep: Looping Through thisPlayersStart.BuildingsParent -> child.gameObject.TryGetComponent(out FactionUnitMarker unitMarker) ");
                                                if (thisFactionData.FactionBuildings.ElementAtOrDefault(unitMarker.unitIndexToSpawn).IsValid())
                                                {
                                                    // Here we create an instance for this iteration, you can control all it's functions and props before spawning it
                                                    IUnit spawnMe = thisFactionData.FactionUnits.ElementAtOrDefault(unitMarker.unitIndexToSpawn).Unit;
                                                    if (ShowDebug)
                                                        Debug.Log("Scene Prep: ElementAtOrDefault(unitMarker.unitIndexToSpawn).Unit " + spawnMe);
                                                    if (spawnMe != null)
                                                    {
                                                        // Spawning just the unit
                                                        this.UnitMgr.CreateUnit(
                                                            spawnMe,
                                                            child.position,
                                                            child.rotation,
                                                            new InitUnitParameters
                                                            {
                                                                factionID = facSlot.ID,
                                                                giveInitResources = true,
                                                                playerCommand = false
                                                            }
                                                        );
                                                        if (ShowDebug)
                                                            Debug.Log("Scene Prep: Success we have placed a Unit -> " + UnitMgr.ToString());
                                                    }
                                                } else
                                                {
                                                    if (ShowDebug)
                                                        Debug.LogWarning("Scene Prep: No faction unit set in ScenePrepManager or too many unit markers for the amount of units to spawn" +
                                                        "Look at the PlayerStartPosition no: " + i);
                                                }
                                            }
                                        }
                                    } // end for loop
                                }
                                break;
                            }
                           /* else
                            {
                                if (ShowDebug)
                                    Debug.LogError("Scene Prep Manager: Faction Initial Cam Look at Position is not set correctly for Player Position: "+ this.PlayerPositionsParent.GetChild(i).position);
                            }*/
                        }
                        else
                        {
                            if (ShowDebug)
                                Debug.Log("Scene Prep: this.PlayerPositionsParent.GetChild(i).TryGetComponent :: Failed");
                        }
                        
                    } // end for loop
                }
            }
        }
    }
    public void Disable()
    {
        this.GameMgr.GameStartRunning -= HandleGameStartRunning;
    }
}
[Serializable]
public class AvailableFaction
{
    public FactionTypeInfo FactionType = null;
    public List<FactionBuilding> FactionBuildings = new();
    public List<FactionUnit> FactionUnits = new();
    public AvailableFaction(
        FactionTypeInfo FactionType,
        List<FactionBuilding> FactionBuildings,
        List<FactionUnit> FactionUnits
    )
    {
        this.FactionType = FactionType;
        this.FactionBuildings = FactionBuildings;
        this.FactionUnits = FactionUnits;
    }
}

[Serializable]
public class FactionBuilding
{
    public Building Building = null;
    public int StartingHealth = 0;
    public FactionBuilding(Building Building, int StartingHealth)
    {
        this.Building = Building;
        this.StartingHealth = StartingHealth;
    }
}

[Serializable]
public class FactionUnit
{
    public Unit Unit = null;
    public FactionUnit(Unit Unit)
    {
        this.Unit = Unit;
    }
}
