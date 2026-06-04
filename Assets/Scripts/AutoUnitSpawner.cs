using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.UnitExtension;
using RTSEngine.Determinism;

/// <summary>
/// Passive auto-spawner for buildings. Every <period> seconds it walks the
/// <resourceGates> list in priority order and uses the first resource whose
/// faction amount is at or above (minimumLevel + costPerSpawn). It then
/// consumes costPerSpawn of that resource and spawns one unit.
///
/// Setup:
///   1. Add this component to a building prefab root.
///   2. Assign Unit Prefab.
///   3. Add one or more entries to Resource Gates in priority order, e.g.:
///        [0]  type=grain   minimumLevel=10  costPerSpawn=5
///        [1]  type=meat    minimumLevel=5   costPerSpawn=3
///      The first gate whose threshold is met is used; the rest are skipped.
///   4. Set Period (seconds between attempts).
///   5. Optionally assign Spawn Transform; defaults to the building position.
/// </summary>
public class AutoUnitSpawner : MonoBehaviour, IEntityPreInitializable
{
    [Header("Unit")]
    [SerializeField, Tooltip("Unit prefab to spawn automatically.")]
    private Unit unitPrefab = null;

    [SerializeField, Tooltip("Optional spawn origin. Defaults to this building's position if not assigned.")]
    private Transform spawnTransform = null;

    [SerializeField, Tooltip("Search radius around the spawn origin for a valid NavMesh position.")]
    private float spawnRadius = 6f;

    [Header("Timer")]
    [SerializeField, Tooltip("Seconds between each spawn attempt.")]
    private float period = 30f;

    [SerializeField, Tooltip("Checked in order. The first gate whose threshold is met triggers a spawn.")]
    private List<ResourceGate> resourceGates = new List<ResourceGate>();

    // Runtime references
    private IFactionEntity factionEntity;
    private IResourceManager resourceMgr;
    private IUnitManager unitMgr;
    private TimeModifiedTimer timer;

    public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
    {
        factionEntity = entity as IFactionEntity;
        resourceMgr = gameMgr.GetService<IResourceManager>();
        unitMgr = gameMgr.GetService<IUnitManager>();
        timer = new TimeModifiedTimer(period);
    }

    public void Disable() { }

    private void Update()
    {
        if (factionEntity == null || !factionEntity.IsInitialized || factionEntity.Health.IsDead)
            return;

        if (unitPrefab == null || resourceGates.Count == 0)
            return;

        if (!timer.ModifiedDecrease())
            return;

        // Walk gates in priority order; use the first one that clears the threshold.
        foreach (ResourceGate gate in resourceGates)
        {
            if (gate.type == null)
                continue;

            int required = gate.minimumLevel + gate.costPerSpawn;

            if (required > 0)
            {
                var check = new ResourceInput
                {
                    type = gate.type,
                    value = new ResourceTypeValue { amount = required, capacity = 0 }
                };

                if (!resourceMgr.HasResources(new ResourceInput[] { check }, factionEntity.FactionID))
                    continue;
            }

            // This gate qualifies — consume its cost and spawn.
            if (gate.costPerSpawn > 0)
            {
                resourceMgr.UpdateResource(
                    factionEntity.FactionID,
                    new ResourceInput[]
                    {
                        new ResourceInput
                        {
                            type = gate.type,
                            value = new ResourceTypeValue { amount = gate.costPerSpawn, capacity = 0 }
                        }
                    },
                    add: false);
            }

            Vector3 origin = spawnTransform != null ? spawnTransform.position : transform.position;
            Vector3 spawnPos = SampleNavMesh(origin, spawnRadius);
            if (spawnPos == Vector3.zero)
                spawnPos = origin;

            unitMgr.CreateUnit(
                unitPrefab,
                spawnPos,
                Quaternion.identity,
                new InitUnitParameters
                {
                    factionID = factionEntity.FactionID,
                    free = false,
                    giveInitResources = false,
                    playerCommand = false
                });

            timer.Reload();
            return; // One unit per tick regardless of how many gates qualify.
        }

        // No gate qualified — still reload so the next attempt waits a full period.
        timer.Reload();
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
