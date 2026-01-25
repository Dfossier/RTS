using RTSEngine.EntityComponent;
using RTSEngine.Entities;
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

        // Safety check: If this is a free unit (no faction), ensure the components are disabled
        if (unit != null && unit.IsFree)
        {
            if (resourceGenerator != null)
            {
                var generatorComponent = resourceGenerator as MonoBehaviour;
                if (generatorComponent != null)
                {
                    generatorComponent.enabled = false;
                    Debug.Log($"[EnableGeneratorOnConversion] ResourceGenerator disabled for free unit '{unit.Code}' - will enable on conversion");
                }
            }

            if (resourceCollector != null)
            {
                var collectorComponent = resourceCollector as MonoBehaviour;
                if (collectorComponent != null)
                {
                    collectorComponent.enabled = false;
                    Debug.Log($"[EnableGeneratorOnConversion] ResourceCollector disabled for free unit '{unit.Code}' - will enable on conversion");
                }
            }
        }

        // If the unit already has a faction (not free), enable the components immediately
        if (unit != null && !unit.IsFree)
        {
            if (resourceGenerator != null)
            {
                var generatorComponent = resourceGenerator as MonoBehaviour;
                if (generatorComponent != null && !generatorComponent.enabled)
                {
                    generatorComponent.enabled = true;
                    hasBeenConverted = true;
                    Debug.Log($"[EnableGeneratorOnConversion] ResourceGenerator enabled for '{unit.Code}' - already owned by Faction {unit.FactionID}");
                }
            }

            if (resourceCollector != null)
            {
                var collectorComponent = resourceCollector as MonoBehaviour;
                if (collectorComponent != null && !collectorComponent.enabled)
                {
                    collectorComponent.enabled = true;
                    hasBeenConverted = true;
                    Debug.Log($"[EnableGeneratorOnConversion] ResourceCollector enabled for '{unit.Code}' - already owned by Faction {unit.FactionID}");
                }
            }
        }
    }

    void Update()
    {
        // Once converted (no longer free), enable the resource components
        if (!hasBeenConverted && unit != null && !unit.IsFree)
        {
            hasBeenConverted = true;

            if (resourceGenerator != null)
            {
                var generatorComponent = resourceGenerator as MonoBehaviour;
                if (generatorComponent != null)
                {
                    generatorComponent.enabled = true;
                    Debug.Log($"[EnableGeneratorOnConversion] ResourceGenerator enabled for '{unit.Code}' - converted to Faction {unit.FactionID}");
                }
            }

            if (resourceCollector != null)
            {
                var collectorComponent = resourceCollector as MonoBehaviour;
                if (collectorComponent != null)
                {
                    collectorComponent.enabled = true;
                    Debug.Log($"[EnableGeneratorOnConversion] ResourceCollector enabled for '{unit.Code}' - converted to Faction {unit.FactionID}");
                }
            }
        }
    }
}
