using RTSEngine;
using RTSEngine.Determinism;
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
///
/// For pure Resource entities (no building phase):
///   - Growth begins immediately after the entity is initialized.
/// </summary>
public class ResourceGrowth : MonoBehaviour
{
    [Header("Growth Settings")]
    [SerializeField, Tooltip("Resource amount when growth begins (after construction for buildings). Clamped to >= 1.")]
    private int startingAmount = 1;

    [SerializeField, Tooltip("Amount to add per tick")]
    private int amountPerTick = 1;

    [SerializeField, Tooltip("Game-time seconds between each tick (scaled by game speed, same as all other engine timers)")]
    private float growthInterval = 30f;

    [SerializeField, Tooltip("Stop growing once the resource is full")]
    private bool stopAtMax = true;

    [Header("Debug")]
    [SerializeField]
    private bool debugMode = false;

    // Max frames to wait for IsInitialized before giving up (avoids infinite hang
    // if the entity was placed in-scene without going through the RTS Engine).
    private const int INIT_TIMEOUT_FRAMES = 500;

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
        yield return null;  // wait one frame for engine's Awake/OnEnable to run

        resource = GetComponent<IResource>();
        if (resource == null)
        {
            Debug.LogError($"[ResourceGrowth] {gameObject.name}: no IResource component found.");
            enabled = false;
            yield break;
        }

        // Poll IsInitialized with a timeout so we never hang indefinitely.
        // IsInitialized becomes true inside Building.CompleteInit(), which is called
        // by Place() during the normal engine spawn path. If the entity was dropped
        // into the scene without going through the engine, this will time out.
        int waitFrames = 0;
        while (!resource.IsInitialized)
        {
            if (++waitFrames > INIT_TIMEOUT_FRAMES)
            {
                Debug.LogError($"[ResourceGrowth] {gameObject.name}: timed out waiting for IsInitialized " +
                               $"after {INIT_TIMEOUT_FRAMES} frames. Was this entity spawned outside the RTS Engine?");
                enabled = false;
                yield break;
            }
            yield return null;
        }

        resourceHealth = resource.Health;
        if (resourceHealth == null)
        {
            Debug.LogError($"[ResourceGrowth] {gameObject.name}: resource.Health returned null. " +
                           $"Is there an IResourceHealth component on the prefab?");
            enabled = false;
            yield break;
        }

        // Warn early if growth can never succeed due to inspector misconfiguration.
        if (!resourceHealth.CanIncrease)
            Debug.LogWarning($"[ResourceGrowth] {gameObject.name}: ResourceHealth.CanIncrease is false — " +
                             $"growth ticks will always fail with 'healthNoIncrease'.");

        building = resource as IBuilding;

        if (building == null)
        {
            // Pure resource (tree, ore vein, etc.) — no construction phase.
            wasBuilt = true;
            if (debugMode)
                Debug.Log($"[ResourceGrowth] {gameObject.name}: not a building resource. " +
                          $"Growth starts immediately at {resourceHealth.CurrHealth}/{resourceHealth.MaxHealth}.");
        }
        else if (building.IsBuilt)
        {
            // Building was already constructed before Initialize() ran
            // (e.g. isBuilt=true in initParams, or BuildingHealth.initialHealth==maxHealth).
            // wasBuilt stays false so Update() still runs the reset on its first frame.
            if (debugMode)
                Debug.Log($"[ResourceGrowth] {gameObject.name}: building already constructed at init. " +
                          $"Reset to startingAmount will fire on first Update.");
        }
        else
        {
            if (debugMode)
                Debug.Log($"[ResourceGrowth] {gameObject.name}: waiting for construction to complete.");
        }

        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
        if (resourceHealth.IsDead) return;

        // For building resources: poll IsBuilt every frame.
        if (building != null)
        {
            if (!building.IsBuilt) return;

            // First frame where IsBuilt becomes true — reset resource to startingAmount.
            if (!wasBuilt)
            {
                wasBuilt = true;
                timer = 0f; // start the growth timer fresh from this moment

                // startingAmount must be >= 1 to avoid reducing health to 0 which
                // triggers Destroy() inside EntityHealth.AddLocal.
                int clampedStart = Mathf.Max(1, startingAmount);
                int delta = clampedStart - resourceHealth.CurrHealth;

                if (delta != 0)
                {
                    if (delta < 0)
                    {
                        // Health is higher than intended startingAmount.
                        // Note: AddLocal with a negative value also fires hit VFX/audio and
                        // sets ResourceHealth.collected=true (which activates the collectedState
                        // visual). This is an edge case — under normal conditions delta==0
                        // because nothing changes ResourceHealth during construction.
                        Debug.LogWarning($"[ResourceGrowth] {gameObject.name}: ResourceHealth was " +
                                         $"{resourceHealth.CurrHealth} at construction end (expected {clampedStart}). " +
                                         $"Forcing reset via AddLocal — hit VFX/audio may fire.");
                    }

                    resourceHealth.AddLocal(new HealthUpdateArgs(delta, null), force: true);
                }

                if (debugMode)
                    Debug.Log($"[ResourceGrowth] {gameObject.name}: construction complete — " +
                              $"resource at {resourceHealth.CurrHealth}/{resourceHealth.MaxHealth} " +
                              $"(delta applied: {delta}). Growing +{amountPerTick} every {growthInterval}s.");
            }
        }

        if (stopAtMax && resourceHealth.HasMaxHealth) return;

        // Scale by the engine's time modifier so growth respects game speed
        // (same pattern as TimeModifiedTimer.ModifiedDecrease in the RTS Engine).
        timer += Time.deltaTime * TimeModifier.CurrentModifier;
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
                Debug.LogWarning($"[ResourceGrowth] {gameObject.name}: growth failed ({result}). " +
                                 $"Check CanIncrease and IsDead on the ResourceHealth component.");
        }
    }
}
