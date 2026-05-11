using System;

using RTSEngine;
using RTSEngine.Determinism;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Game;
using RTSEngine.Health;

using UnityEngine;

/// <summary>
/// Grows a resource's amount over time by simultaneously increasing both
/// ResourceHealth.MaxHealth and ResourceHealth.CurrHealth each tick.
///
/// The resource starts at 1/1 (max=1, current=1) and grows tick by tick until
/// MaxHealth reaches growthMax (e.g. 15). Current health always equals max health
/// between ticks, so the resource is "full" at every intermediate stage — but
/// there is still room to grow because MaxHealth < growthMax.
///
/// When a collector harvests some wheat (reduces CurrHealth), MaxHealth is
/// unaffected. Growth resumes on the next tick and adds to current only (max is
/// already at the right level for that stage). If CurrHealth hits 0 the
/// ResourceBuilding self-destructs (destroyObject = true on ResourceHealth).
///
/// For ResourceBuildings (e.g. horticultural plot):
///   - Growth is blocked until construction completes.
///   - On BuildingBuilt, BuildingHealth.CanIncrease is set to false so no builder
///     can ever re-target this plot through Builder.IsTargetValid().
///   - Worker stopping is handled cleanly by MustStopProgress() on the next
///     TargetUpdate() tick (HasMaxHealth=true → Stop()).
///
/// For pure Resource entities (no building phase):
///   - Growth begins immediately after the entity is initialized.
/// </summary>
public class ResourceGrowth : MonoBehaviour, IEntityPreInitializable
{
    protected IGameManager GameMgr { get; private set; }
    [Header("Growth Settings")]
    [SerializeField, Tooltip("Resource amount (and starting max) when growth begins. Clamped to >= 1.")]
    private int startingAmount = 1;

    [SerializeField, Tooltip("Amount added to both MaxHealth and CurrHealth per tick.")]
    private int amountPerTick = 1;

    [SerializeField, Tooltip("Game-time seconds between each tick (scaled by game speed).")]
    private float growthInterval = 30f;

    [SerializeField, Tooltip("The final maximum the resource can reach. Growth stops when MaxHealth >= growthMax.")]
    private int growthMax = 15;

    [SerializeField, Tooltip("Stop growing once MaxHealth reaches growthMax.")]
    private bool stopAtMax = true;

    [Header("Debug")]
    [SerializeField]
    private bool debugMode = false;

    // Max frames to wait for IsInitialized before giving up.
    private const int INIT_TIMEOUT_FRAMES = 500;

    private IResource resource;
    private IBuilding building;
    private IEntityHealth resourceHealth;
    private float timer = 0f;
    private bool initialized = false;
    private bool wasBuilt = false;

    /// <summary>
    /// Called by BiomePlotModifier (or any external script) in Awake() to override
    /// the inspector defaults before the growth coroutine reads them.
    /// </summary>
    public void SetGrowthParameters(int newGrowthMax, float newGrowthInterval)
    {
        growthMax = Mathf.Max(1, newGrowthMax);
        growthInterval = Mathf.Max(0.01f, newGrowthInterval);
    }

    void Start()
    {

    }
    public void OnEntityPreInit(IGameManager gameMgr,IEntity entity)
    {
        if(entity.IsInitialized)
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

        building = resource as IBuilding;

        if (building == null)
        {
            // Pure resource (tree, ore vein, etc.) — no construction phase.
            wasBuilt = true;
            if (debugMode)
                Debug.Log($"[ResourceGrowth] {gameObject.name}: not a building resource. " +
                          $"Growth starts immediately at {resourceHealth.CurrHealth}/{resourceHealth.MaxHealth} (growthMax={growthMax}).");
        }
        else if (building.IsBuilt)
        {
            // Building was already constructed before Initialize() ran.
            // wasBuilt stays false so Update() fires the one-time setup.
            if (debugMode)
                Debug.Log($"[ResourceGrowth] {gameObject.name}: building already constructed at init. " +
                          $"Setup will fire on first Update.");
        }
        else
        {
            building.BuildingBuilt += OnBuildingBuilt;
            if (debugMode)
                Debug.Log($"[ResourceGrowth] {gameObject.name}: waiting for construction to complete.");
        }

        initialized = true;
    }

    /// <summary>
    /// Called synchronously by the engine when construction finishes (inside Builder.OnProgress()).
    /// We only set CanIncrease=false here — worker stopping is left to MustStopProgress() which
    /// fires cleanly at the top of the next TargetUpdate() tick.
    /// </summary>
    private void OnBuildingBuilt(IBuilding sender, EventArgs args)
    {
        building.BuildingBuilt -= OnBuildingBuilt;

        if (wasBuilt) return; // guard against double-firing
        wasBuilt = true;
        timer = 0f;

        // Disable construction targeting on this building so no builder can ever re-target it.
        building.Health.CanIncrease = false;

        if (debugMode)
            Debug.Log($"[ResourceGrowth] {gameObject.name}: BuildingBuilt fired — " +
                      $"BuildingHealth.CanIncrease disabled. Workers stop via MustStopProgress on next tick.");

        ApplyStartingAmount();
    }

    void Update()
    {
        if (!initialized) return;
        if (resourceHealth.IsDead) return;

        // Pre-built case: building was already IsBuilt when Initialize ran.
        if (building != null && !wasBuilt)
        {
            if (!building.IsBuilt) return;

            wasBuilt = true;
            timer = 0f;
            building.Health.CanIncrease = false;
            ApplyStartingAmount();
        }

        if (stopAtMax && resourceHealth.MaxHealth >= growthMax) return;

        // Scale by the engine's time modifier so growth respects game speed.
        timer += Time.deltaTime * TimeModifier.CurrentModifier;
        if (timer >= growthInterval)
        {
            timer = 0f;
            Grow();
        }
    }

    private void ApplyStartingAmount()
    {
        // startingAmount must be >= 1 to avoid reducing health to 0 (triggers Destroy in EntityHealth.AddLocal).
        int clampedStart = Mathf.Max(1, startingAmount);
        int delta = clampedStart - resourceHealth.CurrHealth;

        if (delta != 0)
        {
            if (delta < 0)
                Debug.LogWarning($"[ResourceGrowth] {gameObject.name}: ResourceHealth was " +
                                 $"{resourceHealth.CurrHealth} at construction end (expected {clampedStart}). " +
                                 $"Forcing reset via AddLocal — hit VFX/audio may fire.");

            resourceHealth.AddLocal(new HealthUpdateArgs(delta, null), force: true);
        }

        if (debugMode)
            Debug.Log($"[ResourceGrowth] {gameObject.name}: construction complete — " +
                      $"resource at {resourceHealth.CurrHealth}/{resourceHealth.MaxHealth} " +
                      $"(delta: {delta}). Growing +{amountPerTick} (max+curr) every {growthInterval}s until growthMax={growthMax}.");
    }

    private void OnDestroy()
    {
        if (!wasBuilt && building.IsValid())
            building.BuildingBuilt -= OnBuildingBuilt;
    }

    private void Grow()
    {
        if (resourceHealth.MaxHealth >= growthMax) return;

        int newMax = Mathf.Min(resourceHealth.MaxHealth + amountPerTick, growthMax);
        int delta = newMax - resourceHealth.MaxHealth;

        int beforeMax = resourceHealth.MaxHealth;
        int beforeCurr = resourceHealth.CurrHealth;

        // Raise the ceiling first so the subsequent Add() doesn't get clamped.
        resourceHealth.SetMax(new HealthUpdateArgs(newMax, null));

        // Grow current health by the same delta.
        ErrorMessage result = resourceHealth.Add(new HealthUpdateArgs(delta, source: null));

        if (debugMode)
        {
            if (result == ErrorMessage.none)
                Debug.Log($"[ResourceGrowth] {gameObject.name}: " +
                          $"max {beforeMax}→{resourceHealth.MaxHealth}, " +
                          $"curr {beforeCurr}→{resourceHealth.CurrHealth} / {resourceHealth.MaxHealth}");
            else
                Debug.LogWarning($"[ResourceGrowth] {gameObject.name}: current-health grow failed ({result}). " +
                                 $"MaxHealth was raised to {resourceHealth.MaxHealth}.");
        }
    }

    public void Disable()
    {

    }
}
