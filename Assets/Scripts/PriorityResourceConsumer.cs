using UnityEngine;
using UnityEngine.Events;
using RTSEngine.Entities;
using RTSEngine.Game;
using RTSEngine.ResourceExtension;
using RTSEngine.Determinism;

/// <summary>
/// Consumes resources from a priority-ordered list each period.
/// On each tick the component walks down the list and consumes from the
/// first entry whose resource the faction currently has enough of.
/// If nothing in the list is available the onStarvation event fires.
///
/// Setup:
///   1. Remove (or disable) the ResourceGenerator used for food consumption.
///   2. Add this component to the villager prefab root.
///   3. Fill in 'Consumption List' in priority order, e.g.:
///        [0] grain   amount 1
///        [1] berries amount 1
///        [2] meat    amount 1
///   4. Set 'Period' to the consumption interval in seconds.
///   5. Optionally wire up 'On Starvation' to a UI warning or health penalty.
/// </summary>
public class PriorityResourceConsumer : MonoBehaviour, IEntityPreInitializable
{
    [SerializeField, Tooltip("Resources to try consuming each period, in priority order.")]
    private ResourceInput[] consumptionList = new ResourceInput[0];

    [SerializeField, Tooltip("Seconds between each consumption tick.")]
    private float period = 5f;

    [SerializeField, Tooltip("Fired when none of the resources in the list are available.")]
    private UnityEvent onStarvation = new UnityEvent();

    private IFactionEntity factionEntity;
    private IResourceManager resourceMgr;
    private TimeModifiedTimer timer;

    public void OnEntityPreInit(IGameManager gameMgr, IEntity entity)
    {
        factionEntity = entity as IFactionEntity;
        resourceMgr = gameMgr.GetService<IResourceManager>();
        timer = new TimeModifiedTimer(period);
    }

    public void Disable() { }

    private void Update()
    {
        if (factionEntity == null || !factionEntity.IsInitialized || factionEntity.Health.IsDead)
            return;

        if (!timer.ModifiedDecrease())
            return;

        // Walk the list and consume the first resource the faction can afford
        foreach (ResourceInput entry in consumptionList)
        {
            if (resourceMgr.HasResources(new ResourceInput[] { entry }, factionEntity.FactionID))
            {
                resourceMgr.UpdateResource(factionEntity.FactionID, new ResourceInput[] { entry }, add: false);
                timer.Reload();
                return;
            }
        }

        // Nothing was available
        onStarvation.Invoke();
        timer.Reload();
    }
}
