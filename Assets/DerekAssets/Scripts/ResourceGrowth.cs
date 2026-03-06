using RTSEngine;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Health;

using UnityEngine;

/// <summary>
/// Grows a resource's amount over time by adding to its ResourceHealth at regular intervals.
///
/// For ResourceBuildings (e.g. horticultural plot):
///   - Growth is blocked until construction completes (polls building.IsBuilt each frame).
///   - The moment IsBuilt becomes true, the resource is forced to 'startingAmount' so it
///     always begins at a known low value regardless of what the engine did during construction.
///   - Growth then proceeds from startingAmount up to the ResourceHealth max.
/// </summary>
public class ResourceGrowth : MonoBehaviour
{
    [Header("Growth Settings")]
    [SerializeField, Tooltip("Resource amount when growth begins (after construction for buildings)")]
    private int startingAmount = 1;

    [SerializeField, Tooltip("Amount to add per tick")]
    private int amountPerTick = 1;

    [SerializeField, Tooltip("Seconds between each tick")]
    private float growthInterval = 5f;

    [SerializeField, Tooltip("Stop growing once the resource is full")]
    private bool stopAtMax = true;

    [Header("Debug")]
    [SerializeField]
    private bool debugMode = false;

    private IResource resource;
    private IBuilding building;
    private IEntityHealth resourceHealth;
    private float timer = 0f;
    private bool initialized = false;
    private bool wasBuilt = false;

    void Start()
    {
        StartCoroutine(Initialize());
    }

    private System.Collections.IEnumerator Initialize()
    {
        yield return null;

        resource = GetComponent<IResource>();
        if (resource == null)
        {
            Debug.LogError($"[ResourceGrowth] {gameObject.name}: no IResource component found.");
            enabled = false;
            yield break;
        }

        while (!resource.IsInitialized)
            yield return null;

        resourceHealth = resource.Health;
        if (resourceHealth == null)
        {
            Debug.LogError($"[ResourceGrowth] {gameObject.name}: no ResourceHealth found.");
            enabled = false;
            yield break;
        }

        building = resource as IBuilding;

        // Non-building resources have no construction phase — treat as already built
        if (building == null)
            wasBuilt = true;

        initialized = true;

        if (debugMode)
            Debug.Log($"[ResourceGrowth] {gameObject.name}: initialized. " +
                      $"IsBuilding={building != null}, IsBuilt={building?.IsBuilt ?? true}");
    }

    void Update()
    {
        if (!initialized) return;
        if (resourceHealth.IsDead) return;

        // For building resources, poll IsBuilt each frame
        if (building != null)
        {
            if (!building.IsBuilt) return;

            // First frame where IsBuilt becomes true — reset resource to startingAmount
            if (!wasBuilt)
            {
                wasBuilt = true;

                int delta = startingAmount - resourceHealth.CurrHealth;
                if (delta != 0)
                    resourceHealth.AddLocal(new HealthUpdateArgs(delta, null), force: true);

                if (debugMode)
                    Debug.Log($"[ResourceGrowth] {gameObject.name}: construction complete — " +
                              $"resource reset to {resourceHealth.CurrHealth}/{resourceHealth.MaxHealth}, " +
                              $"growing +{amountPerTick} every {growthInterval}s");
            }
        }

        if (stopAtMax && resourceHealth.HasMaxHealth) return;

        timer += Time.deltaTime;
        if (timer >= growthInterval)
        {
            timer = 0f;
            Grow();
        }
    }

    private void Grow()
    {
        int toAdd = Mathf.Min(amountPerTick, resourceHealth.MaxHealth - resourceHealth.CurrHealth);
        if (toAdd <= 0) return;

        int before = resourceHealth.CurrHealth;
        ErrorMessage result = resourceHealth.Add(new HealthUpdateArgs(toAdd, source: null));

        if (debugMode)
        {
            if (result == ErrorMessage.none)
                Debug.Log($"[ResourceGrowth] {gameObject.name}: {before} → {resourceHealth.CurrHealth}/{resourceHealth.MaxHealth}");
            else
                Debug.LogWarning($"[ResourceGrowth] {gameObject.name}: growth failed ({result})");
        }
    }
}
