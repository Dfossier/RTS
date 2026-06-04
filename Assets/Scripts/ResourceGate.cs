using System;
using UnityEngine;
using RTSEngine.ResourceExtension;

/// <summary>
/// One entry in AutoUnitSpawner's resource gate list.
/// </summary>
[Serializable]
public class ResourceGate
{
    [Tooltip("Resource type to check and optionally consume.")]
    public ResourceTypeInfo type;

    [Tooltip("The faction must hold at least this much of the resource AFTER paying the cost. " +
        "Set to 0 to only require the cost itself.")]
    public int minimumLevel;

    [Tooltip("Amount consumed each time a unit is spawned. Set to 0 for free spawning.")]
    public int costPerSpawn;
}
