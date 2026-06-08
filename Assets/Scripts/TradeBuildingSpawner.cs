using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using RTSEngine.Game;
using RTSEngine.Entities;
using RTSEngine.BuildingExtension;

/// <summary>
/// Spawns four neutral ("free", factionID -1) trade buildings, one at the midpoint of each map edge
/// (North / East / West / South - NOT the corners). Each building sells a direction-specific
/// specialty plus food/wood, bartered instantly via <see cref="ResourceTraderComponent"/>.
///
///   North -> Amber      East -> Lapis Lazuli      West -> Tin      South -> Food & Wood
///
/// One prefab is reused for all four; the per-direction offer lists below are injected into each
/// spawned building's trader component, so all trade rates are edited here in one place.
///
/// Self-driven: it waits (in Update) until the RTS Engine GameManager is active and its building
/// service is ready - the same "wait until active" pattern <see cref="AnimalsSpawnController"/> uses -
/// then spawns once and disables itself.
/// </summary>
public class TradeBuildingSpawner : MonoBehaviour
{
    [Header("References (auto-found if left empty)")]
    [Tooltip("Source of the map size + the RTS GameManager object. Auto-found in the scene if null.")]
    public DerekTerrainManager terrainManager;
    [Tooltip("The trade building prefab. Its root (or a child) must have a ResourceTraderComponent.")]
    public GameObject tradeBuildingPrefab;
    [Tooltip("Map center. If null, uses the 'middleOfTheMap' object, else world origin.")]
    public Transform mapCenter;

    [Header("Placement")]
    [Tooltip("How far IN from the very edge to place each building (world units). Keeps them off the map border / sea.")]
    public float edgeInset = 30f;
    [Tooltip("If the inset edge point isn't on valid ground/NavMesh, step this far further inward and retry.")]
    public float inwardStepDistance = 15f;
    [Tooltip("Max inward retry steps before giving up on a direction.")]
    public int maxInwardSteps = 25;
    [Tooltip("Height above the probe point to raycast down from, to find the terrain surface.")]
    public float rayHeight = 400f;
    [Tooltip("NavMesh sample radius when snapping the found ground point onto the NavMesh.")]
    public float navSampleRadius = 12f;
    [Tooltip("Used only if the terrain map size can't be read. (width X, depth Z) in world units.")]
    public Vector2 manualMapSizeFallback = new Vector2(800f, 800f);

    [Header("Offers per direction (pay cost -> get reward)")]
    [Tooltip("North building: Amber for sale + food/wood. Configure rates here.")]
    public TradeOffer[] northOffers = new TradeOffer[0];
    [Tooltip("East building: Lapis Lazuli for sale + food/wood.")]
    public TradeOffer[] eastOffers = new TradeOffer[0];
    [Tooltip("West building: Tin for sale + food/wood.")]
    public TradeOffer[] westOffers = new TradeOffer[0];
    [Tooltip("South building: food + wood.")]
    public TradeOffer[] southOffers = new TradeOffer[0];

    private bool spawned = false;
    private IGameManager gameMgr;
    private IBuildingManager buildingMgr;

    private void Update()
    {
        if (spawned)
            return;

        if (!TryResolveServices())
            return; // not ready yet - try again next frame

        SpawnAllTradeBuildings();
        spawned = true;
        enabled = false; // stop ticking once done
    }

    /// <summary> Resolves the GameManager + building service once the engine is up. Returns false until ready. </summary>
    private bool TryResolveServices()
    {
        if (terrainManager == null)
            terrainManager = FindObjectOfType<DerekTerrainManager>();
        if (terrainManager == null)
            return false;

        GameObject gmObject = terrainManager.GameManager;
        if (gmObject == null || !gmObject.activeInHierarchy)
            return false;

        gameMgr = gmObject.GetComponent<GameManager>();
        if (gameMgr == null)
            return false;

        // Wait until the engine has finished building & starting the game. At this point all
        // services are registered, faction slots exist, and GetService is safe to call.
        if (gameMgr.State != GameStateType.running)
            return false;

        buildingMgr = gameMgr.GetService<IBuildingManager>();
        return buildingMgr != null;
    }

    private void SpawnAllTradeBuildings()
    {
        if (tradeBuildingPrefab == null)
        {
            Debug.LogError("[TradeBuildingSpawner] No trade building prefab assigned - nothing spawned.", this);
            return;
        }

        IBuilding prefabBuilding = tradeBuildingPrefab.GetComponent<IBuilding>();
        if (prefabBuilding == null)
        {
            Debug.LogError("[TradeBuildingSpawner] The trade building prefab has no IBuilding component.", this);
            return;
        }

        Vector3 center = mapCenter != null ? mapCenter.position : ResolveMapCenter();
        GetHalfExtents(out float halfWidth, out float halfDepth);

        // Inset edge midpoints (clamped so the inset never crosses the center).
        float xEdge = Mathf.Max(0f, halfWidth - edgeInset);
        float zEdge = Mathf.Max(0f, halfDepth - edgeInset);

        SpawnOne(center + new Vector3(0f, 0f, zEdge), center, northOffers, prefabBuilding, "North");  // +Z
        SpawnOne(center + new Vector3(xEdge, 0f, 0f), center, eastOffers, prefabBuilding, "East");    // +X
        SpawnOne(center + new Vector3(-xEdge, 0f, 0f), center, westOffers, prefabBuilding, "West");   // -X
        SpawnOne(center + new Vector3(0f, 0f, -zEdge), center, southOffers, prefabBuilding, "South"); // -Z
    }

    private void SpawnOne(Vector3 edgePoint, Vector3 center, TradeOffer[] offers, IBuilding prefabBuilding, string label)
    {
        if (!TryGetGroundNavPosition(edgePoint, center, out Vector3 spawnPos))
        {
            Debug.LogWarning($"[TradeBuildingSpawner] Couldn't find valid ground/NavMesh for the {label} trade building.", this);
            return;
        }

        IBuilding building = buildingMgr.CreatePlacedBuildingLocal(
            prefabBuilding,
            spawnPos,
            Quaternion.identity,
            new InitBuildingParameters
            {
                factionID = -1,        // neutral
                free = true,           // belongs to no faction
                setInitialHealth = false,
                giveInitResources = false,
                buildingCenter = null, // no territory/border
                isBuilt = true,        // spawn fully constructed
                playerCommand = false
            });

        if (building == null)
        {
            Debug.LogWarning($"[TradeBuildingSpawner] Failed to spawn the {label} trade building.", this);
            return;
        }

        // Inject this direction's offers into the building's trader component.
        ResourceTraderComponent trader = building.gameObject.GetComponentInChildren<ResourceTraderComponent>();
        if (trader != null)
            trader.SetOffers(offers);
        else
            Debug.LogWarning($"[TradeBuildingSpawner] {label} trade building has no ResourceTraderComponent; offers not set.", this);
    }

    /// <summary>
    /// Finds a valid spawn point near <paramref name="edgePoint"/>: raycasts down to the terrain,
    /// snaps to the NavMesh, and if that fails steps inward toward the center and retries.
    /// </summary>
    private bool TryGetGroundNavPosition(Vector3 edgePoint, Vector3 center, out Vector3 result)
    {
        result = Vector3.zero;

        Vector3 inward = center - edgePoint;
        inward.y = 0f;
        inward = inward.sqrMagnitude > 0.0001f ? inward.normalized : Vector3.zero;

        for (int step = 0; step < maxInwardSteps; step++)
        {
            Vector3 probe = edgePoint + inward * (step * inwardStepDistance);
            Vector3 rayOrigin = new Vector3(probe.x, center.y + rayHeight, probe.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayHeight * 2f)
                && NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navSampleRadius, NavMesh.AllAreas))
            {
                result = navHit.position;
                return true;
            }
        }

        return false;
    }

    private void GetHalfExtents(out float halfWidth, out float halfDepth)
    {
        TerrainGenerator gen = terrainManager != null ? terrainManager.TGenerator : null;
        if (gen != null && gen.MeshWorldSize > 0f)
        {
            Vector3 size = gen.MapWorldSize;
            halfWidth = size.x * 0.5f;
            halfDepth = size.z * 0.5f;
        }
        else
        {
            halfWidth = manualMapSizeFallback.x * 0.5f;
            halfDepth = manualMapSizeFallback.y * 0.5f;
        }
    }

    private Vector3 ResolveMapCenter()
    {
        GameObject middle = GameObject.Find("middleOfTheMap");
        return middle != null ? middle.transform.position : Vector3.zero;
    }
}
