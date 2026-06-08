using RTSEngine.EntityComponent;
using RTSEngine.Entities;
using RTSEngine.Event;
using UnityEngine;

/// <summary>
/// Enables the ResourceGenerator and ResourceCollector components on a unit only after it has been converted to a faction.
/// This prevents errors when these components try to access faction resources while the unit is still free/neutral.
///
/// Usage:
/// 1. Attach this script to any unit prefab that has ResourceGenerator and/or ResourceCollector components
/// 2. Ensure those components are DISABLED by default in the prefab
/// 3. When the unit gets converted to a faction, this script will automatically enable them
/// </summary>
public class EnableGeneratorOnConversion : MonoBehaviour
{
    private IUnit unit;
    private IResourceGenerator resourceGenerator;
    private IResourceCollector resourceCollector;
    private bool hasBeenConverted = false;

    void Start()
    {
        // Get references to the unit and resource components
        unit = GetComponent<IUnit>();
        resourceGenerator = GetComponent<IResourceGenerator>();
        resourceCollector = GetComponent<IResourceCollector>();

        if (unit == null)
            return;

        if (unit.IsFree)
        {
            // Free/neutral unit: keep the resource components disabled and wait for a faction conversion.
            // Subscribe to the engine's faction-change event instead of polling IsFree every frame.
            DisableResourceComponents();
            unit.FactionUpdateComplete += OnFactionUpdateComplete;
        }
        else
        {
            // Unit already belongs to a faction: enable the resource components immediately.
            EnableResourceComponents($"already owned by Faction {unit.FactionID}");
        }
    }

    void OnDestroy()
    {
        // The engine event holds a reference to this handler, so always unsubscribe.
        if (unit != null)
            unit.FactionUpdateComplete -= OnFactionUpdateComplete;
    }

    // Raised by the RTS Engine when this unit's faction changes (IsFree/FactionID are already updated at this point).
    private void OnFactionUpdateComplete(IEntity sender, FactionUpdateArgs args)
    {
        if (hasBeenConverted || unit == null || unit.IsFree)
            return;

        EnableResourceComponents($"converted to Faction {unit.FactionID}");

        // One-shot: once the components are enabled there's nothing left to listen for.
        unit.FactionUpdateComplete -= OnFactionUpdateComplete;
    }

    private void EnableResourceComponents(string reason)
    {
        hasBeenConverted = true;

        var generatorComponent = resourceGenerator as MonoBehaviour;
        if (generatorComponent != null && !generatorComponent.enabled)
        {
            generatorComponent.enabled = true;
            Debug.Log($"[EnableGeneratorOnConversion] ResourceGenerator enabled for '{unit.Code}' - {reason}");
        }

        var collectorComponent = resourceCollector as MonoBehaviour;
        if (collectorComponent != null && !collectorComponent.enabled)
        {
            collectorComponent.enabled = true;
            Debug.Log($"[EnableGeneratorOnConversion] ResourceCollector enabled for '{unit.Code}' - {reason}");
        }
    }

    private void DisableResourceComponents()
    {
        var generatorComponent = resourceGenerator as MonoBehaviour;
        if (generatorComponent != null)
        {
            generatorComponent.enabled = false;
            Debug.Log($"[EnableGeneratorOnConversion] ResourceGenerator disabled for free unit '{unit.Code}' - will enable on conversion");
        }

        var collectorComponent = resourceCollector as MonoBehaviour;
        if (collectorComponent != null)
        {
            collectorComponent.enabled = false;
            Debug.Log($"[EnableGeneratorOnConversion] ResourceCollector disabled for free unit '{unit.Code}' - will enable on conversion");
        }
    }
}
