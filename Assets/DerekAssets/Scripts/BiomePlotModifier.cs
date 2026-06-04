using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to a horticultural plot (or any building with ResourceGrowth + GrowTreeController).
///
/// On Start (after one FixedUpdate), casts a ray straight down to sample the terrain
/// mesh's UV3 channel, which stores (heat, moisture) per-vertex — the same values the
/// terrain shader uses to determine biome.  The matching BiomeProfile is then applied:
///   - ResourceGrowth  : growthMax (max harvestable HP) and growthInterval (seconds per tick)
///   - GrowTreeController : growthDelay (seconds before visual grow animation starts)
///
/// Sampling is deferred to Start + WaitForFixedUpdate so that terrain MeshColliders
/// are fully registered with the physics engine before the raycast fires.
///
/// Biome thresholds mirror the terrain shader exactly:
///   Tundra   : heat  > 0.6
///   Desert   : heat  < 0.2  and moisture < 0.3
///   Jungle   : heat  < 0.2  and moisture >= 0.3
///   Steppe   : 0.2 <= heat <= 0.6  and moisture < 0.4
///   Grassland: 0.2 <= heat <= 0.6  and moisture >= 0.4
/// </summary>
public class BiomePlotModifier : MonoBehaviour
{
    [System.Serializable]
    public struct BiomeProfile
    {
        [Tooltip("Maximum harvestable HP the crop can reach (ResourceGrowth.growthMax).")]
        public int growthMax;

        [Tooltip("Seconds between each HP tick (ResourceGrowth.growthInterval). Shorter = faster growth.")]
        public float growthInterval;

        [Tooltip("Seconds to wait after construction before the crop visually grows (GrowTreeController delay).")]
        public float growthDelay;
    }

    [Header("Biome Profiles")]
    [Tooltip("Fertile temperate land — highest yield, fastest growth.")]
    public BiomeProfile grassland    = new BiomeProfile { growthMax = 15, growthInterval = 5f,  growthDelay = 5f  };

    [Tooltip("Hot and wet — good yield, slightly slower than grassland.")]
    public BiomeProfile jungle       = new BiomeProfile { growthMax = 12, growthInterval = 7f,  growthDelay = 8f  };

    [Tooltip("Temperate but dry — moderate yield and growth.")]
    public BiomeProfile steppe       = new BiomeProfile { growthMax = 10, growthInterval = 10f, growthDelay = 12f };

    [Tooltip("Hot and dry — poor soil, low yield, slow growth.")]
    public BiomeProfile desert       = new BiomeProfile { growthMax = 6,  growthInterval = 15f, growthDelay = 20f };

    [Tooltip("Cold — very low yield, very slow growth.")]
    public BiomeProfile tundra       = new BiomeProfile { growthMax = 4,  growthInterval = 20f, growthDelay = 30f };

    [Tooltip("Fallback used when no terrain hit is found (e.g. placed over water).")]
    public BiomeProfile defaultProfile = new BiomeProfile { growthMax = 10, growthInterval = 10f, growthDelay = 15f };

    [Header("Sampling")]
    [Tooltip("How far above the building to start the downward ray.")]
    public float rayOriginHeight = 50f;

    [Tooltip("Maximum distance the ray will travel downward.")]
    public float rayMaxDistance = 200f;

    [Tooltip("Layer mask for the terrain mesh — terrain chunks use layer 8 by default.")]
    public LayerMask terrainLayer = 1 << 8;

    [Header("Debug")]
    public bool debugMode = false;

    IEnumerator Start()
    {
        // Wait one FixedUpdate so terrain MeshColliders are registered with the physics engine.
        yield return new WaitForFixedUpdate();

        BiomeProfile profile;
        string biomeName;
        if (TrySampleBiome(out Vector2 biomeUV))
        {
            float heat     = biomeUV.x;
            float moisture = biomeUV.y;
            (profile, biomeName) = ClassifyBiome(heat, moisture);
        }
        else
        {
            profile   = defaultProfile;
            biomeName = "Default (no terrain hit)";
        }

        ResourceGrowth growth = GetComponent<ResourceGrowth>();
        if (growth != null)
            growth.SetGrowthParameters(profile.growthMax, (int)profile.growthInterval);
        else if (debugMode)
            Debug.LogWarning($"[BiomePlotModifier] {gameObject.name}: no ResourceGrowth component found — HP not modified.");

        GrowTreeController growCtrl = GetComponent<GrowTreeController>();
        if (growCtrl != null)
        {
            growCtrl.SetGrowthDelay(profile.growthDelay);
            growCtrl.ResourceMax = profile.growthMax;
        }
        else if (debugMode)
            Debug.LogWarning($"[BiomePlotModifier] {gameObject.name}: no GrowTreeController component found — visual delay not modified.");

        if (debugMode)
            Debug.Log($"[BiomePlotModifier] {gameObject.name}: biome={biomeName}  growthMax={profile.growthMax}  growthInterval={profile.growthInterval}s  growthDelay={profile.growthDelay}s");
    }

    /// <summary>
    /// Casts a ray downward and reads mesh.uv3 (heat, moisture) from the terrain triangle hit.
    /// Returns false if no terrain collider was hit.
    /// </summary>
    private bool TrySampleBiome(out Vector2 biomeUV)
    {
        biomeUV = Vector2.zero;

        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        Ray ray = new Ray(origin, Vector3.down);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, terrainLayer))
        {
            if (debugMode)
                Debug.LogWarning($"[BiomePlotModifier] {gameObject.name}: raycast found no terrain below {transform.position}.");
            return false;
        }

        MeshCollider mc = hit.collider as MeshCollider;
        if (mc == null || mc.sharedMesh == null)
        {
            if (debugMode)
                Debug.LogWarning($"[BiomePlotModifier] {gameObject.name}: terrain hit but collider is not a MeshCollider.");
            return false;
        }

        Mesh mesh = mc.sharedMesh;
        Vector2[] uv3 = mesh.uv3;

        if (uv3 == null || uv3.Length == 0)
        {
            if (debugMode)
                Debug.LogWarning($"[BiomePlotModifier] {gameObject.name}: mesh has no uv3 channel.");
            return false;
        }

        // Interpolate biome UV across the hit triangle using barycentric coordinates.
        int[] tris = mesh.triangles;
        int i = hit.triangleIndex * 3;
        Vector3 b = hit.barycentricCoordinate;
        biomeUV = uv3[tris[i]] * b.x + uv3[tris[i + 1]] * b.y + uv3[tris[i + 2]] * b.z;

        if (debugMode)
            Debug.Log($"[BiomePlotModifier] {gameObject.name}: sampled heat={biomeUV.x:F3}  moisture={biomeUV.y:F3}");

        return true;
    }

    /// <summary>
    /// Applies the same biome classification logic as the terrain shader.
    /// </summary>
    private (BiomeProfile profile, string name) ClassifyBiome(float heat, float moisture)
    {
        if (heat > 0.6f)
            return (tundra, "Tundra");

        if (heat < 0.2f)
            return moisture < 0.3f
                ? (desert, "Desert")
                : (jungle,  "Jungle");

        // Temperate zone (0.2 <= heat <= 0.6)
        return moisture < 0.4f
            ? (steppe,    "Steppe")
            : (grassland, "Grassland");
    }
}
