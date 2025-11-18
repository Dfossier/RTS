using UnityEngine;
using DerekAssets.EntityComponent;
using RTSEngine.EntityComponent;

namespace DerekAssets.Examples
{
    /// <summary>
    /// Example script demonstrating how to use WorkforceUnitCarrier programmatically.
    /// This can be attached to any entity with a WorkforceUnitCarrier to provide
    /// runtime control and monitoring.
    /// </summary>
    public class WorkforceUnitCarrierExample : MonoBehaviour
    {
        [Header("Example Configuration")]
        [SerializeField, Tooltip("Reference to the WorkforceUnitCarrier component.")]
        private WorkforceUnitCarrier workforceCarrier;

        [SerializeField, Tooltip("Example: Upgrade force multiplier when research is completed.")]
        private float upgradedForceMultiplier = 1.5f;

        [SerializeField, Tooltip("Example: Enable efficiency monitoring.")]
        private bool enableEfficiencyMonitoring = true;

        [SerializeField, Tooltip("Example: Log workforce changes.")]
        private bool logWorkforceChanges = true;

        // Runtime efficiency tracking
        private float totalResourcesGenerated = 0f;
        private float totalTimeWithWorkforce = 0f;
        private float lastCheckTime;

        #region Unity Events
        private void Start()
        {
            // Auto-find WorkforceUnitCarrier if not assigned
            if (workforceCarrier == null)
                workforceCarrier = GetComponent<WorkforceUnitCarrier>();

            if (workforceCarrier == null)
            {
                Debug.LogError($"[{name}] No WorkforceUnitCarrier found! Please assign one or add to this entity.");
                return;
            }

            // Subscribe to workforce events
            if (logWorkforceChanges)
            {
                workforceCarrier.UnitAdded += OnWorkforceAdded;
                workforceCarrier.UnitRemoved += OnWorkforceRemoved;
            }

            lastCheckTime = Time.time;

            Debug.Log($"[{name}] WorkforceUnitCarrierExample initialized. Current workforce: {workforceCarrier.WorkforceCount}");
        }

        private void Update()
        {
            if (enableEfficiencyMonitoring && workforceCarrier != null)
                UpdateEfficiencyTracking();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (workforceCarrier != null && logWorkforceChanges)
            {
                workforceCarrier.UnitAdded -= OnWorkforceAdded;
                workforceCarrier.UnitRemoved -= OnWorkforceRemoved;
            }
        }
        #endregion

        #region Event Handlers
        private void OnWorkforceAdded(RTSEngine.EntityComponent.IUnitCarrier sender, RTSEngine.Event.UnitCarrierEventArgs args)
        {
            Debug.Log($"[{name}] Worker added: {args.Unit.Name}. " +
                     $"Workforce: {workforceCarrier.WorkforceCount}/{workforceCarrier.MaxAmount}. " +
                     $"Speed: {workforceCarrier.CurrentSpeedMultiplier:F2}x");

            // Example: Play sound effect, show UI notification, etc.
            OnWorkforceChanged();
        }

        private void OnWorkforceRemoved(RTSEngine.EntityComponent.IUnitCarrier sender, RTSEngine.Event.UnitCarrierEventArgs args)
        {
            Debug.Log($"[{name}] Worker removed: {args.Unit.Name}. " +
                     $"Workforce: {workforceCarrier.WorkforceCount}/{workforceCarrier.MaxAmount}. " +
                     $"Speed: {workforceCarrier.CurrentSpeedMultiplier:F2}x");

            OnWorkforceChanged();
        }

        private void OnWorkforceChanged()
        {
            // Example: Update UI, trigger effects, adjust other systems
            
            // Example efficiency bonus for full workforce
            if (workforceCarrier.WorkforceCount == workforceCarrier.MaxAmount)
            {
                Debug.Log($"[{name}] Maximum workforce achieved! Consider implementing bonus effects.");
            }
            
            // Example warning for empty workforce
            if (workforceCarrier.WorkforceCount == 0)
            {
                Debug.Log($"[{name}] No workforce! Production has stopped.");
            }
        }
        #endregion

        #region Efficiency Monitoring
        private void UpdateEfficiencyTracking()
        {
            float currentTime = Time.time;
            float deltaTime = currentTime - lastCheckTime;

            // Track time with active workforce
            if (workforceCarrier.WorkforceCount > 0)
            {
                totalTimeWithWorkforce += deltaTime;
                
                // Estimate resources that would be generated (simplified calculation)
                if (workforceCarrier.CurrentPeriod > 0)
                {
                    float resourcesPerSecond = 1f / workforceCarrier.CurrentPeriod;
                    totalResourcesGenerated += resourcesPerSecond * deltaTime;
                }
            }

            lastCheckTime = currentTime;
        }

        public float GetEfficiencyRatio()
        {
            float totalTime = Time.time;
            return totalTime > 0 ? totalTimeWithWorkforce / totalTime : 0f;
        }

        public float GetEstimatedResourceGeneration()
        {
            return totalResourcesGenerated;
        }
        #endregion

        #region Example Runtime Controls
        [Header("Runtime Controls (Example Methods)")]
        [SerializeField, Tooltip("Test button: Apply efficiency upgrade")]
        private bool applyEfficiencyUpgrade = false;

        [SerializeField, Tooltip("Test button: Reset efficiency tracking")]
        private bool resetEfficiencyTracking = false;

        [SerializeField, Tooltip("Test button: Log current status")]
        private bool logCurrentStatus = false;

        private void OnValidate()
        {
            // Example runtime controls (for testing in inspector)
            if (Application.isPlaying && workforceCarrier != null)
            {
                if (applyEfficiencyUpgrade)
                {
                    ApplyEfficiencyUpgrade();
                    applyEfficiencyUpgrade = false;
                }

                if (resetEfficiencyTracking)
                {
                    ResetEfficiencyTracking();
                    resetEfficiencyTracking = false;
                }

                if (logCurrentStatus)
                {
                    LogCurrentStatus();
                    logCurrentStatus = false;
                }
            }
        }

        /// <summary>
        /// Example: Apply an efficiency upgrade that increases the force multiplier.
        /// This could be called when research is completed, building is upgraded, etc.
        /// </summary>
        public void ApplyEfficiencyUpgrade()
        {
            if (workforceCarrier == null) return;

            workforceCarrier.SetForceMultiplier(upgradedForceMultiplier);
            Debug.Log($"[{name}] Efficiency upgrade applied! New force multiplier: {upgradedForceMultiplier}");
        }

        /// <summary>
        /// Example: Reset efficiency tracking (useful for game events, building repairs, etc.)
        /// </summary>
        public void ResetEfficiencyTracking()
        {
            totalResourcesGenerated = 0f;
            totalTimeWithWorkforce = 0f;
            lastCheckTime = Time.time;
            Debug.Log($"[{name}] Efficiency tracking reset.");
        }

        /// <summary>
        /// Example: Log comprehensive status information.
        /// </summary>
        public void LogCurrentStatus()
        {
            if (workforceCarrier == null) return;

            Debug.Log($"[{name}] Status Report:\n" +
                     $"Workforce: {workforceCarrier.WorkforceCount}/{workforceCarrier.MaxAmount}\n" +
                     $"Speed Multiplier: {workforceCarrier.CurrentSpeedMultiplier:F2}x\n" +
                     $"Current Period: {workforceCarrier.CurrentPeriod:F2}s\n" +
                     $"Efficiency Ratio: {GetEfficiencyRatio():F2} ({GetEfficiencyRatio() * 100:F1}%)\n" +
                     $"Estimated Resources Generated: {GetEstimatedResourceGeneration():F2}");
        }
        #endregion

        #region Integration Examples
        /// <summary>
        /// Example: Integrate with a research system
        /// </summary>
        public void OnResearchCompleted(string researchID)
        {
            switch (researchID)
            {
                case "WorkerEfficiency":
                    workforceCarrier.SetForceMultiplier(workforceCarrier.CurrentSpeedMultiplier * 1.25f);
                    Debug.Log($"[{name}] Worker efficiency research completed!");
                    break;
                    
                case "AutomatedSystems":
                    // Example: Reduce workforce requirement
                    Debug.Log($"[{name}] Automated systems research completed!");
                    break;
            }
        }

        /// <summary>
        /// Example: Integrate with building damage system
        /// </summary>
        public void OnBuildingDamaged(float damagePercent)
        {
            // Reduce efficiency based on damage
            float efficiencyLoss = damagePercent * 0.5f; // 50% efficiency loss at 100% damage
            float reducedMultiplier = upgradedForceMultiplier * (1f - efficiencyLoss);
            workforceCarrier.SetForceMultiplier(Mathf.Max(0.1f, reducedMultiplier));
            
            Debug.Log($"[{name}] Building damaged! Efficiency reduced to {(1f - efficiencyLoss) * 100:F1}%");
        }

        /// <summary>
        /// Example: Integrate with faction resource system
        /// </summary>
        public void OnResourceCapacityChanged()
        {
            // Example: Adjust generation based on storage capacity
            // This could automatically pause production when storage is full
            Debug.Log($"[{name}] Resource capacity changed. Consider adjusting production rates.");
        }
        #endregion
    }
}