using RTSEngine;
using RTSEngine.Entities;
using RTSEngine.Event;
using RTSEngine.Health;

using UnityEngine;

/// <summary>
/// Allows resources to grow over time by increasing their health at regular intervals.
/// Also scales the visual model based on current health percentage (e.g., wheat starts small and grows).
///
/// Perfect for farming mechanics where crops grow from planted state to harvestable state.
///
/// Usage:
/// 1. Attach to a Resource GameObject (must have IResource and IResourceHealth components)
/// 2. Assign the visualModel (the GameObject to scale - usually a child with the mesh)
/// 3. Configure growth settings in Inspector
/// 4. Resource will grow from minScale to maxScale as health increases from 0 to MaxHealth
/// </summary>
public class ResourceGrowth : MonoBehaviour
{
    #region Inspector Settings
    [Header("Growth Settings")]
    [SerializeField, Tooltip("Amount of health to add per growth interval")]
    private int healthPerGrowthTick = 5;

    [SerializeField, Tooltip("Time in seconds between each growth tick")]
    private float growthInterval = 2f;

    [SerializeField, Tooltip("Minimum health required to start growing (prevents growth if too depleted)")]
    private int minHealthToGrow = 1;

    [SerializeField, Tooltip("If true, growth stops when max health is reached. If false, continues trying to grow.")]
    private bool stopAtMaxHealth = true;

    [Header("Visual Scaling")]
    [SerializeField, Tooltip("The GameObject to scale (usually a child object with the visual mesh). Leave empty to use this GameObject.")]
    private Transform visualModel;

    [SerializeField, Tooltip("Scale when health is 0 (starting size)")]
    private Vector3 minScale = new Vector3(0.1f, 0.1f, 0.1f);

    [SerializeField, Tooltip("Scale when health is at max (full grown size)")]
    private Vector3 maxScale = new Vector3(1f, 1f, 1f);

    [SerializeField, Tooltip("If true, scale is updated every frame. If false, only updates on growth ticks (better performance).")]
    private bool smoothScaling = true;

    [Header("Debug")]
    [SerializeField, Tooltip("Show debug logs for growth events")]
    private bool debugMode = false;
    #endregion

    #region Private Variables
    private IResource resource;
    private IEntityHealth resourceHealth;
    private float growthTimer = 0f;
    private bool isInitialized = false;
    #endregion

    #region Initialization
    void Start()
    {
        // Delay initialization to ensure RTS Engine has initialized the resource entity first
        StartCoroutine(InitializeAfterEntitySetup());
    }

    private System.Collections.IEnumerator InitializeAfterEntitySetup()
    {
        // Wait one frame to allow RTS Engine to initialize the Resource entity
        yield return null;

        // Get the resource component
        resource = GetComponent<IResource>();

        if (resource == null)
        {
            Debug.LogError($"[ResourceGrowth] No IResource component found on {gameObject.name}. This script requires a Resource component!");
            enabled = false;
            yield break;
        }

        // Get the health component directly instead of relying on resourceHealth
        // This avoids timing issues with RTS Engine's initialization
        resourceHealth = GetComponent<IEntityHealth>();

        if (resourceHealth == null)
        {
            Debug.LogError($"[ResourceGrowth] Resource {gameObject.name} has no Health component!");
            enabled = false;
            yield break;
        }

        // If no visual model assigned, use this GameObject
        if (visualModel == null)
        {
            visualModel = transform;
            if (debugMode)
                Debug.Log($"[ResourceGrowth] No visualModel assigned for {gameObject.name}, using root GameObject");
        }

        // Set initial scale based on current health
        UpdateScale();

        isInitialized = true;

        if (debugMode)
        {
            Debug.Log($"[ResourceGrowth] Initialized on {gameObject.name}");
            Debug.Log($"├─ Current Health: {resourceHealth.CurrHealth}/{resourceHealth.MaxHealth}");
            Debug.Log($"├─ Growth: +{healthPerGrowthTick} every {growthInterval}s");
            Debug.Log($"└─ Scale Range: {minScale} to {maxScale}");
        }
    }
    #endregion

    #region Update Loop
    void Update()
    {
        if (!isInitialized)
            return;

        // Update scale smoothly if enabled
        if (smoothScaling)
            UpdateScale();

        // Don't grow if resource is dead
        if (resourceHealth.IsDead)
            return;

        // Don't grow if at max health (if stopAtMaxHealth is enabled)
        if (stopAtMaxHealth && resourceHealth.HasMaxHealth)
            return;

        // Don't grow if below minimum health threshold
        if (resourceHealth.CurrHealth < minHealthToGrow)
        {
            if (debugMode)
                Debug.Log($"[ResourceGrowth] {gameObject.name} cannot grow - health too low ({resourceHealth.CurrHealth} < {minHealthToGrow})");
            return;
        }

        // Increment growth timer
        growthTimer += Time.deltaTime;

        // Check if it's time to grow
        if (growthTimer >= growthInterval)
        {
            growthTimer = 0f;
            Grow();
        }
    }
    #endregion

    #region Growth Logic
    private void Grow()
    {
        // Calculate how much health to add (don't exceed max)
        int currentHealth = resourceHealth.CurrHealth;
        int maxHealth = resourceHealth.MaxHealth;
        int healthToAdd = Mathf.Min(healthPerGrowthTick, maxHealth - currentHealth);

        if (healthToAdd <= 0)
        {
            if (debugMode)
                Debug.Log($"[ResourceGrowth] {gameObject.name} already at max health ({currentHealth}/{maxHealth})");
            return;
        }

        // Add health using the built-in system
        ErrorMessage result = resourceHealth.Add(new HealthUpdateArgs(healthToAdd, source: null));

        if (result == ErrorMessage.none)
        {
            if (debugMode)
            {
                Debug.Log($"[ResourceGrowth] {gameObject.name} grew!");
                Debug.Log($"├─ Added: +{healthToAdd} health");
                Debug.Log($"├─ Health: {currentHealth} → {resourceHealth.CurrHealth}");
                Debug.Log($"└─ Progress: {(resourceHealth.CurrHealth / (float)maxHealth * 100f):F1}% grown");
            }

            // Update scale if not using smooth scaling
            if (!smoothScaling)
                UpdateScale();
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"[ResourceGrowth] Failed to grow {gameObject.name}: {result}");
        }
    }
    #endregion

    #region Visual Scaling
    private void UpdateScale()
    {
        if (visualModel == null || resource?.Health == null)
            return;

        // Calculate growth percentage (0 to 1)
        float growthPercent = resourceHealth.CurrHealth / (float)resourceHealth.MaxHealth;
        growthPercent = Mathf.Clamp01(growthPercent);

        // Lerp between min and max scale based on health percentage
        Vector3 targetScale = Vector3.Lerp(minScale, maxScale, growthPercent);

        // Apply scale
        if (smoothScaling)
        {
            // Smooth transition
            visualModel.localScale = Vector3.Lerp(visualModel.localScale, targetScale, Time.deltaTime * 2f);
        }
        else
        {
            // Instant snap
            visualModel.localScale = targetScale;
        }
    }
    #endregion

    #region Public Methods (Optional - for external control)
    /// <summary>
    /// Manually trigger growth (useful for events or triggers)
    /// </summary>
    public void TriggerGrowth()
    {
        if (!isInitialized)
            return;

        Grow();
    }

    /// <summary>
    /// Reset growth timer (delays next growth tick)
    /// </summary>
    public void ResetGrowthTimer()
    {
        growthTimer = 0f;
    }

    /// <summary>
    /// Instantly set resource to a specific growth stage (0 to 1)
    /// </summary>
    /// <param name="growthPercent">0 = minimum health/scale, 1 = max health/scale</param>
    public void SetGrowthStage(float growthPercent)
    {
        if (!isInitialized || resource?.Health == null)
            return;

        growthPercent = Mathf.Clamp01(growthPercent);
        int targetHealth = Mathf.RoundToInt(resourceHealth.MaxHealth * growthPercent);

        // Use the SetHealth method if available, otherwise add/subtract to reach target
        int healthDifference = targetHealth - resourceHealth.CurrHealth;
        if (healthDifference != 0)
        {
            resourceHealth.Add(new HealthUpdateArgs(healthDifference, source: null));
            UpdateScale();

            if (debugMode)
                Debug.Log($"[ResourceGrowth] Set {gameObject.name} to {growthPercent * 100f}% growth ({targetHealth}/{resourceHealth.MaxHealth} health)");
        }
    }
    #endregion

    #region Gizmos (Editor Visualization)
    private void OnDrawGizmosSelected()
    {
        // Show growth radius in editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Show min and max scale visualization
        if (visualModel != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(visualModel.position, maxScale);

            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireCube(visualModel.position, minScale);
        }
    }
    #endregion
}
