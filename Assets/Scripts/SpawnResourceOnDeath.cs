using UnityEngine;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;

/// <summary>
/// Attach to an animal unit prefab. On death, spawns a separate carcass Resource prefab
/// at the animal's position so villagers can harvest it.
///
/// Setup:
///   1. Create a standalone Resource prefab (e.g. "deer_carcass") with:
///        - Resource component (unique code like "meat", resource type assigned)
///        - ResourceHealth component
///        - ResourceWorkerManager component
///        - A child model object with EntityModelConnections + EntitySelection
///      You can reuse the deer mesh on this prefab so it looks like the dead body.
///   2. Add this component to the animal prefab root.
///   3. Assign the carcass prefab to the 'Carcass Resource Prefab' field.
///   4. Optionally assign the deer's renderer(s) to 'Renderers To Hide On Death'
///      so the live deer mesh disappears when the carcass spawns.
/// </summary>
public class SpawnResourceOnDeath : MonoBehaviour, IEntityPreInitializable
{
    [SerializeField, Tooltip("Standalone Resource prefab to spawn at the death position.")]
    private Resource carcassResourcePrefab = null;

    [SerializeField, Tooltip("Renderers on the live animal to hide when the carcass spawns (prevents visual overlap).")]
    private Renderer[] renderersToHideOnDeath = new Renderer[0];

    private IEntity entity;
    private IGameManager gameMgr;
    private IResourceManager resourceMgr;

    public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
    {
        this.gameMgr = gameMgr;
        this.entity = entity;
        this.resourceMgr = gameMgr.GetService<IResourceManager>();

        entity.Health.EntityDead += OnEntityDead;
    }

    public void Disable()
    {
        if (entity != null)
            entity.Health.EntityDead -= OnEntityDead;
    }

    private void OnEntityDead(IEntity sender, DeadEventArgs args)
    {
        if (args.IsUpgrade || carcassResourcePrefab == null)
            return;

        // Hide the live animal's renderers so only the carcass prefab is visible
        foreach (Renderer r in renderersToHideOnDeath)
            if (r != null) r.enabled = false;

        // Spawn the carcass resource at the deer's current position
        resourceMgr.CreateResource(
            carcassResourcePrefab,
            transform.position,
            transform.rotation,
            new InitResourceParameters
            {
                free = true,
                factionID = -1,
                playerCommand = false
            }
        );
    }
}
