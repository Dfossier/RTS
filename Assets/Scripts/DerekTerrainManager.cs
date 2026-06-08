using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RTSEngine.Entities;
using UnityEngine;
using UnityEngine.AI;

public class DerekTerrainManager : MonoBehaviour
{
    public TerrainGenerator TGenerator = null;
    public GameObject rtsReEntities;
    public GameObject GameManager;
    private float timer;

    // public GameObject npcFactionPrefab;
    // public GameObject[] npcFactionSpawned;

    public GameObject[] npcFactionsList;

    [Tooltip("Spawns the neutral trade buildings at the map edges. Optional; auto-finds the manager itself if left wired.")]
    public TradeBuildingSpawner tradeBuildingSpawner;

    public void Start()
    {
        InitializeDerekTerrain();
    }

    // run this on rts engine initialization before anything else to set the values, this is only related to the terrain stuff like resources, trees etc and not rts pre configs from the lobby, there will be a different manager for that
    public void InitializeDerekTerrain()
    {
        ReParentingObjects();
        SetPlayerFactionPosition();
        LoadNPCsAndSetPosition();
        GameManager.SetActive(true);

        // Kick off the neutral trade buildings. The spawner waits internally until the GameManager's
        // services are ready, then spawns once at the map edges.
        if (tradeBuildingSpawner != null)
        {
            tradeBuildingSpawner.terrainManager = this;
            tradeBuildingSpawner.enabled = true;
        }
    }

    private void ReParentingObjects()
    {
        // change parent of all resources from derek's ResourceEntities to RTS ResourceEntities
        Transform mapgen = GameObject.Find("Map Generator").transform;

        if(mapgen.TryGetComponent(out TerrainGenerator generator))
        {
            TGenerator = generator;
        }

        if(TGenerator != null && TGenerator.PreLoadResources.Count > 0)
        {
            foreach(Resource rss in TGenerator.PreLoadResources)
            {
                if (rss != null)
                {
                    rss.transform.SetParent(rtsReEntities.transform, true);
                }
            }
        }

    }

    private void SetPlayerFactionPosition()
    {
        GameObject spawnpointObj = GameObject.Find("debugRandomFactionSpawnpoint");
        GameObject playerFaction = GameObject.Find("playerFaction");

        if (spawnpointObj == null || playerFaction == null)
        {
            Debug.LogError("[DerekTerrainManager] 'debugRandomFactionSpawnpoint' or 'playerFaction' not found - cannot position the player faction.");
            return;
        }

        // RandomFactionSpawnpoint already validated this spot (a large, connected NavMesh region).
        // Use it directly - just snap to the NavMesh. Do NOT re-randomise with a big offset, which
        // used to shove the faction into water or, on a failed sample, all the way to world origin.
        Vector3 basePos = SnapToNavMesh(spawnpointObj.transform.position, 8f, fallback: spawnpointObj.transform.position);
        playerFaction.transform.position = basePos;

        // Reposition any direct children (starting buildings/units) onto solid, roomy ground nearby.
        // Each lands in a NavMesh region big enough to be real land, never at the origin.
        foreach (Transform child in playerFaction.transform)
            child.position = FindValidNearbyPosition(basePos, spreadRadius: 6f, minRegion: 20, attempts: 25, fallback: basePos);
    }

    /// <summary>
    /// Snaps a point to the nearest NavMesh position. Returns <paramref name="fallback"/> (never the
    /// origin) if no NavMesh is found even after widening the search.
    /// </summary>
    private Vector3 SnapToNavMesh(Vector3 pos, float sampleRadius, Vector3 fallback)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            return hit.position;
        if (NavMesh.SamplePosition(pos, out hit, sampleRadius * 8f, NavMesh.AllAreas))
            return hit.position;
        return fallback;
    }

    /// <summary>
    /// Finds a NavMesh point near <paramref name="center"/> that sits in a region of at least
    /// <paramref name="minRegion"/> connected cells (i.e. real, open land - not a water-edge sliver).
    /// Falls back to a plain NavMesh snap of the center if no roomy spot is found.
    /// </summary>
    private Vector3 FindValidNearbyPosition(Vector3 center, float spreadRadius, int minRegion, int attempts, Vector3 fallback)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = RandomNavmeshLocation(spreadRadius, center);
            if (candidate == Vector3.zero)
                continue; // RandomNavmeshLocation returns zero when the NavMesh sample fails
            if (IsNavMeshRegionBigEnough(candidate, 4f, minRegion))
                return candidate;
        }
        return SnapToNavMesh(center, spreadRadius, fallback);
    }

    private void LoadNPCsAndSetPosition()
    {
        RandomFactionSpawnpoint factionsController = GameObject.Find("debugRandomFactionSpawnpoint").GetComponent<RandomFactionSpawnpoint>();
        // if (factionsController.NPC_Count <= 0) return;
        /*
        for (int i = 0; i < factionsController.NPC_Count; i++)
        {
            var new_npcFaction = Instantiate(npcFactionPrefab);
            new_npcFaction.transform.position = factionsController.NPCsSpawnpoint[i];
            new_npcFaction.transform.SetParent(GameObject.Find("FactionEntities").transform, true);
            npcFactionSpawned[i] = new_npcFaction;
        }
        */

        // set stuff on RTS Engine GameManager
        // GameManager.GetComponent<GameManager>().FactionSlots[0].Enabled = false;

        for (int i = 0; i < npcFactionsList.Count(); i++)
        {
            // Use the validated NPC spawn point directly (snap to NavMesh); don't re-randomise into
            // water or, on a failed sample, to world origin.
            Vector3 basePos = SnapToNavMesh(factionsController.NPCsSpawnpoint[i], 8f, fallback: factionsController.NPCsSpawnpoint[i]);
            npcFactionsList[i].transform.position = basePos;

            // Each child is a starting entity (incl. the campfire, which is parented under a faction
            // container). Place it on a roomy NavMesh region so buildings don't land in forbidden
            // terrain (the old code used a tiny minSize=5 and could drop children at the origin).
            foreach (Transform child in npcFactionsList[i].transform)
                child.position = FindValidNearbyPosition(basePos, spreadRadius: 6f, minRegion: 20, attempts: 25, fallback: basePos);
        }
    }

    public Vector3 RandomNavmeshLocation(float radius, Vector3 center)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;
        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }
    
    bool IsNavMeshRegionBigEnough(Vector3 startPoint, float step = 4f, int minSize = 20, int maxVisited = 500)
    {
        if (!NavMesh.SamplePosition(startPoint, out NavMeshHit startHit, 2f, NavMesh.AllAreas))
            return false;

        HashSet<Vector3> visited = new();
        Queue<Vector3> queue = new();
        queue.Enqueue(startHit.position);
        visited.Add(startHit.position);

        Vector3[] directions = {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right
        };

        while (queue.Count > 0 && visited.Count < minSize && visited.Count < maxVisited)
        {
            Vector3 current = queue.Dequeue();

            foreach (var dir in directions)
            {
                Vector3 neighbor = current + dir * step;
                if (visited.Contains(neighbor)) continue;

                if (!NavMesh.SamplePosition(neighbor, out NavMeshHit neighborHit, step * 0.5f, NavMesh.AllAreas))
                    continue;

                if (NavMesh.Raycast(current, neighborHit.position, out _, NavMesh.AllAreas))
                    continue;

                visited.Add(neighborHit.position);
                queue.Enqueue(neighborHit.position);

                if (visited.Count >= maxVisited)
                    break;
            }
        }

        return visited.Count >= minSize;
    }
}
