using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using RTSEngine.Entities;
using RTSEngine.Faction;
using RTSEngine.Game;
using RTSEngine.UnitExtension;

/// <summary>
/// Spawns starting units for every active faction at game start.
/// Uses each faction's spawn position (driven by DerekTerrainManager) as the
/// base point and scatters units to nearby valid NavMesh positions.
///
/// Setup:
///   1. Add this component to the ScenePrep child inside RTSEngine.prefab
///      (alongside RTSEScenePrepManager) so the GameManager discovers it.
///   2. Add unit prefabs to 'Starting Units' — add the same prefab multiple
///      times to spawn multiples (e.g. villager_dereks x3).
///   3. Adjust 'Spawn Radius' to control how far from the campfire units appear.
/// </summary>
public class FactionStartingUnitsSpawner : MonoBehaviour, IPreRunGameService
{
    [SerializeField, Tooltip("Unit prefabs to spawn for every faction. Add the same prefab multiple times for multiples.")]
    private List<Unit> startingUnits = new List<Unit>();

    [SerializeField, Tooltip("Search radius around the faction spawn point to find valid NavMesh positions.")]
    private float spawnRadius = 8f;

    private IGameManager gameMgr;
    private IUnitManager unitMgr;

    public void Init(IGameManager gameMgr)
    {
        Debug.Log("[FactionStartingUnitsSpawner] Init() called.");
        this.gameMgr = gameMgr;
        this.unitMgr = gameMgr.GetService<IUnitManager>();
        gameMgr.GameStartRunning += HandleGameStartRunning;
    }

    public void Disable()
    {
        if (gameMgr != null)
            gameMgr.GameStartRunning -= HandleGameStartRunning;
    }

    private void HandleGameStartRunning(IGameManager source, EventArgs args)
    {
        Debug.Log($"[FactionStartingUnitsSpawner] HandleGameStartRunning fired. ActiveFactionCount={source.ActiveFactionCount}, unitPrefabs={startingUnits.Count}");

        if (startingUnits.Count == 0)
        {
            Debug.LogWarning("[FactionStartingUnitsSpawner] No units in Starting Units list — nothing to spawn.");
            return;
        }

        for (int i = 0; i < source.ActiveFactionCount; i++)
        {
            FactionSlot slot = (FactionSlot)source.FactionSlots.ElementAt(i);
            Vector3 basePos = slot.FactionSpawnPosition;
            //Debug.Log($"[FactionStartingUnitsSpawner] Faction {i} (ID={slot.ID}) spawnPos={basePos}");

            foreach (Unit prefab in startingUnits)
            {
                if (prefab == null)
                {
                    Debug.LogWarning($"[FactionStartingUnitsSpawner] Null entry in Starting Units list for faction {i} — skipping.");
                    continue;
                }

                Vector3 spawnPos = SampleNavMesh(basePos, spawnRadius);
                if (spawnPos == Vector3.zero)
                {
                    Debug.LogWarning($"[FactionStartingUnitsSpawner] NavMesh sample failed for faction {i}, falling back to spawn point.");
                    spawnPos = basePos;
                }

                //Debug.Log($"[FactionStartingUnitsSpawner] Spawning '{prefab.name}' for faction {i} at {spawnPos}");
                unitMgr.CreateUnit(
                    prefab,
                    spawnPos,
                    Quaternion.identity,
                    new InitUnitParameters
                    {
                        factionID = slot.ID,
                        giveInitResources = false,
                        playerCommand = false
                    }
                );
            }
        }
    }

    private Vector3 SampleNavMesh(Vector3 center, float radius)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 candidate = center + UnityEngine.Random.insideUnitSphere * radius;
            candidate.y = center.y;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return Vector3.zero;
    }
}
