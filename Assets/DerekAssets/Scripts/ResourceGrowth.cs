using System;
using System.Collections;
using UnityEngine;
using RTSEngine;
using RTSEngine.Game;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Health;
using RTSEngine.UI;

public class ResourceGrowth : MonoBehaviour, IEntityPreInitializable
{
    [Header("Growth Settings")]
    [Tooltip("HP added to the resource each growth tick")]
    public int amountPerTick = 1;

    [Tooltip("Seconds between each growth tick")]
    public float growthInterval = 5f;

    [Tooltip("Maximum HP the resource can reach (overridden by BiomePlotModifier)")]
    public int growthMax = 15;

    [Tooltip("If true, growth starts immediately for testing")]
    public bool debugMode = false;

    [Tooltip("If true, show hover tooltip with growth progress. Disable for trees and visuals.")]
    public bool showTooltip = false;

    private IResource resource;
    private IResourceHealth resourceHealth;
    private Coroutine growthCoroutine;
    private bool growthComplete;
    private float nextGrowthTickTime;  // Time.time when next tick occurs
    private IGameManager gameMgr;
    private IGlobalEventPublisher globalEvent;
    private bool isMouseHovering = false;

    public bool GrowthComplete => growthComplete;

    public void OnEntityPreInit(IGameManager gameManager, IEntity entity)
    {
        this.gameMgr = gameManager;
        growthCoroutine = StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        resource = GetComponent<IResource>();
        if (resource == null)
        {
            Debug.LogError($"[{gameObject.name}] ResourceGrowth: No IResource component found!");
            yield break;
        }

        // Wait for resource to finish its own initialization
        while (!resource.IsInitialized)
            yield return null;

        resourceHealth = resource.Health;
        if (resourceHealth == null)
        {
            Debug.LogError($"[{gameObject.name}] ResourceGrowth: No IResourceHealth component found!");
            yield break;
        }

        if (showTooltip)
        {
            globalEvent = gameMgr.GetService<IGlobalEventPublisher>();
            globalEvent.EntityMouseEnterGlobal += HandleMouseEnter;
            globalEvent.EntityMouseExitGlobal += HandleMouseExit;
        }

        ApplyStartingAmount();
        StartCoroutine(GrowthTimer());
    }

    private void ApplyStartingAmount()
    {
        // Ensure max health capacity is at least growthMax
        if (resourceHealth.MaxHealth < growthMax)
        {
            resourceHealth.SetMaxLocal(new HealthUpdateArgs(growthMax, resource));
            Debug.Log($"[{gameObject.name}] ResourceGrowth: MaxHealth set to {growthMax}");
        }

        // Resource starts at 1 HP - set it if it is not already
        if (resourceHealth.CurrHealth < 1)
        {
            resourceHealth.AddLocal(new HealthUpdateArgs(1 - resourceHealth.CurrHealth, resource));
        }

        // Resource is always collectable - no blocking
        Debug.Log($"[{gameObject.name}] ResourceGrowth: Started at {resourceHealth.CurrHealth}/{resourceHealth.MaxHealth}. Grows +{amountPerTick} every {growthInterval}s up to {growthMax}. Always harvestable.");
    }

    private IEnumerator GrowthTimer()
    {
        while (!growthComplete)
        {
            nextGrowthTickTime = Time.time + growthInterval;
            yield return new WaitForSeconds(growthInterval);
            OnGrowthTick();
        }
    }

    private void OnGrowthTick()
    {
        if (resourceHealth == null)
            return;

        if (resourceHealth.IsDead)
        {
            Debug.Log($"[{gameObject.name}] ResourceGrowth: Resource is dead, stopping growth.");
            growthComplete = true;
            return;
        }

        // Reset growthComplete if resource was harvested below growthMax
        if (growthComplete && resourceHealth.CurrHealth < growthMax)
        {
            growthComplete = false;
            Debug.Log($"[{gameObject.name}] ResourceGrowth: Harvest detected ({resourceHealth.CurrHealth}/{growthMax}). Resuming growth.");
        }

        if (growthComplete)
            return;

        if (resourceHealth.CurrHealth < growthMax)
        {
            int amountToAdd = Mathf.Min(amountPerTick, growthMax - resourceHealth.CurrHealth);
            resourceHealth.AddLocal(new HealthUpdateArgs(amountToAdd, resource));
            RaiseGrowthTooltip();
            Debug.Log($"[{gameObject.name}] ResourceGrowth: +{amountToAdd} -> Curr={resourceHealth.CurrHealth}/{growthMax}");
        }

        if (resourceHealth.CurrHealth >= growthMax)
        {
            growthComplete = true;
            Debug.Log($"[{gameObject.name}] ResourceGrowth: Reached max ({growthMax}). Growth complete.");
        }
    }

    public void Disable()
    {
        if (growthCoroutine != null)
        {
            StopCoroutine(growthCoroutine);
            growthCoroutine = null;
        }
    }

    private void HandleMouseEnter(IEntity entity, EventArgs e)
    {
        isMouseHovering = true;
        RaiseGrowthTooltip();
    }

    private void HandleMouseExit(IEntity entity, EventArgs e)
    {
        isMouseHovering = false;
    }

    private void OnDestroy()
    {
        if (growthCoroutine != null)
            StopCoroutine(growthCoroutine);
        
        if (globalEvent != null)
        {
            globalEvent.EntityMouseEnterGlobal -= HandleMouseEnter;
            globalEvent.EntityMouseExitGlobal -= HandleMouseExit;
        }
    }

    /// <summary>Called by BiomePlotModifier to set biome-specific growth parameters.</summary>
    public void SetGrowthParameters(float interval, int max)
    {
        growthInterval = interval;
        growthMax = max;
        // Sync ResourceHealth max capacity to match growth cap
        if (resourceHealth != null)
        {
            resourceHealth.SetMaxLocal(new HealthUpdateArgs(growthMax, resource));
        }
        Debug.Log($"[{gameObject.name}] ResourceGrowth: Biome -> interval={interval}s, max={max}");
    }

    /// <summary>Get the underlying resource health component (for UI overlays).</summary>
    public IResourceHealth GetResourceHealth() => resourceHealth;

    /// <summary>Seconds remaining until the next growth tick. Returns 0 if growth is complete or not started.</summary>
    public float GetNextGrowthTime()
    {
        if (growthComplete)
            return 0f;
        if (nextGrowthTickTime <= 0f)
            return growthInterval;
        float remaining = nextGrowthTickTime - Time.time;
        return Mathf.Max(0f, remaining);
    }

    /// <summary>Raise a tooltip showing growth progress (only if showTooltip is enabled).</summary>
    private void RaiseGrowthTooltip()
    {
        if (!showTooltip || globalEvent == null || resourceHealth == null)
            return;

        int curr = resourceHealth.CurrHealth;
        int max = growthMax;
        float next = GetNextGrowthTime();

        string tooltip;
        if (growthComplete)
            tooltip = $"Growth complete: {curr}/{max} HP";
        else
            tooltip = $"Growing: {curr}/{max} HP ({next:F1}s until next)";

        globalEvent.RaiseShowTooltipGlobal(this, new MessageEventArgs(MessageType.info, tooltip));
    }
}
