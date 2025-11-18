using UnityEngine;
using System.Reflection;
using RTSEngine.EntityComponent;
using RTSEngine.Entities;
using RTSEngine.Event;

namespace DerekAssets.EntityComponent
{
    /// <summary>
    /// Extension of UnitCarrier that modifies a ResourceGenerator based on the number of units carried.
    /// Units act as workforce that boosts resource generation speed and enables/disables the generator.
    /// </summary>
    public class WorkforceUnitCarrier : UnitCarrier
    {
        [Header("Workforce Resource Generation")]
        [SerializeField, Tooltip("The ResourceGenerator component to modify based on carried units.")]
        private ResourceGenerator targetResourceGenerator;

        [SerializeField, Tooltip("Force multiplier for resource generation speed. 100% means 1.0x multiplier per unit."), Range(0.1f, 5.0f)]
        private float forceMultiplier = 1.0f;

        [SerializeField, Tooltip("Enable debug logging for workforce calculations.")]
        private bool enableDebugLog = false;

        // Reflection fields for accessing private ResourceGenerator members
        private FieldInfo periodField;
        private FieldInfo isActiveField;
        
        // Store original values
        private float originalPeriod;
        private bool originalIsActive;
        private bool hasStoredOriginals = false;

        #region Initialization
        protected override void OnInit()
        {
            base.OnInit();

            // Validate ResourceGenerator reference
            if (targetResourceGenerator == null)
            {
                // Try to find ResourceGenerator on the same entity
                targetResourceGenerator = Entity.GetComponent<ResourceGenerator>();
                
                if (targetResourceGenerator == null)
                {
                    logger.LogError($"[{GetType().Name} - {Entity.Code}] No ResourceGenerator component found! Please assign one in the inspector or add one to the same entity.");
                    return;
                }
            }

            // Setup reflection to access private fields in ResourceGenerator
            SetupReflection();

            // Store original values
            StoreOriginalValues();

            // Subscribe to unit events
            UnitAdded += OnWorkforceUnitAdded;
            UnitRemoved += OnWorkforceUnitRemoved;

            // Apply initial workforce effect
            UpdateResourceGenerator();

            if (enableDebugLog)
                logger.Log($"[{GetType().Name} - {Entity.Code}] WorkforceUnitCarrier initialized. Target ResourceGenerator: {targetResourceGenerator.name}, Force Multiplier: {forceMultiplier}");
        }

        protected override void OnDisabled()
        {
            // Unsubscribe from events
            UnitAdded -= OnWorkforceUnitAdded;
            UnitRemoved -= OnWorkforceUnitRemoved;

            // Restore original ResourceGenerator state
            RestoreOriginalValues();

            base.OnDisabled();
        }
        #endregion

        #region Reflection Setup
        private void SetupReflection()
        {
            System.Type resourceGeneratorType = typeof(ResourceGenerator);
            
            // Get private period field
            periodField = resourceGeneratorType.GetField("period", BindingFlags.NonPublic | BindingFlags.Instance);
            if (periodField == null)
            {
                logger.LogError($"[{GetType().Name} - {Entity.Code}] Could not find 'period' field in ResourceGenerator via reflection!");
                return;
            }

            // We'll use the IsActive property instead of trying to access private fields for active state
        }

        private void StoreOriginalValues()
        {
            if (targetResourceGenerator == null || periodField == null)
                return;

            originalPeriod = (float)periodField.GetValue(targetResourceGenerator);
            originalIsActive = targetResourceGenerator.IsActive;
            hasStoredOriginals = true;

            if (enableDebugLog)
                logger.Log($"[{GetType().Name} - {Entity.Code}] Stored original values - Period: {originalPeriod}, IsActive: {originalIsActive}");
        }

        private void RestoreOriginalValues()
        {
            if (!hasStoredOriginals || targetResourceGenerator == null || periodField == null)
                return;

            periodField.SetValue(targetResourceGenerator, originalPeriod);
            targetResourceGenerator.SetActiveLocal(originalIsActive, false);

            if (enableDebugLog)
                logger.Log($"[{GetType().Name} - {Entity.Code}] Restored original values - Period: {originalPeriod}, IsActive: {originalIsActive}");
        }
        #endregion

        #region Event Handlers
        private void OnWorkforceUnitAdded(IUnitCarrier sender, UnitCarrierEventArgs args)
        {
            UpdateResourceGenerator();
            
            if (enableDebugLog)
                logger.Log($"[{GetType().Name} - {Entity.Code}] Unit added: {args.Unit.Name}. Current workforce: {CurrAmount}/{MaxAmount}");
        }

        private void OnWorkforceUnitRemoved(IUnitCarrier sender, UnitCarrierEventArgs args)
        {
            UpdateResourceGenerator();
            
            if (enableDebugLog)
                logger.Log($"[{GetType().Name} - {Entity.Code}] Unit removed: {args.Unit.Name}. Current workforce: {CurrAmount}/{MaxAmount}");
        }
        #endregion

        #region Resource Generator Modification
        private void UpdateResourceGenerator()
        {
            if (targetResourceGenerator == null || periodField == null || !hasStoredOriginals)
                return;

            int unitCount = CurrAmount;
            
            // Set active state based on whether we have units
            bool shouldBeActive = unitCount > 0;
            
            if (targetResourceGenerator.IsActive != shouldBeActive)
            {
                targetResourceGenerator.SetActiveLocal(shouldBeActive, false);
                
                if (enableDebugLog)
                    logger.Log($"[{GetType().Name} - {Entity.Code}] ResourceGenerator active state changed to: {shouldBeActive}");
            }

            // Calculate new period based on workforce
            if (unitCount > 0)
            {
                // Formula: new period = original period / (multiplier * number of units)
                float workforceMultiplier = forceMultiplier * unitCount;
                float newPeriod = originalPeriod / workforceMultiplier;
                
                // Ensure period doesn't go below a reasonable minimum (0.1 seconds)
                newPeriod = Mathf.Max(newPeriod, 0.1f);
                
                periodField.SetValue(targetResourceGenerator, newPeriod);
                
                if (enableDebugLog)
                {
                    float speedBoost = originalPeriod / newPeriod;
                    logger.Log($"[{GetType().Name} - {Entity.Code}] Updated ResourceGenerator - Units: {unitCount}, Original Period: {originalPeriod:F2}s, New Period: {newPeriod:F2}s, Speed Boost: {speedBoost:F2}x");
                }
            }
            else
            {
                // No units, restore original period but keep inactive
                periodField.SetValue(targetResourceGenerator, originalPeriod);
                
                if (enableDebugLog)
                    logger.Log($"[{GetType().Name} - {Entity.Code}] No workforce - ResourceGenerator disabled, period restored to: {originalPeriod:F2}s");
            }
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// Gets the current workforce count (number of carried units).
        /// </summary>
        public int WorkforceCount => CurrAmount;

        /// <summary>
        /// Gets the current resource generation speed multiplier.
        /// </summary>
        public float CurrentSpeedMultiplier
        {
            get
            {
                if (CurrAmount == 0) return 0f;
                return forceMultiplier * CurrAmount;
            }
        }

        /// <summary>
        /// Gets the current effective period for resource generation.
        /// </summary>
        public float CurrentPeriod
        {
            get
            {
                if (CurrAmount == 0 || !hasStoredOriginals) return originalPeriod;
                return originalPeriod / CurrentSpeedMultiplier;
            }
        }

        /// <summary>
        /// Sets the force multiplier at runtime.
        /// </summary>
        /// <param name="newMultiplier">New force multiplier value</param>
        public void SetForceMultiplier(float newMultiplier)
        {
            forceMultiplier = Mathf.Max(0.1f, newMultiplier);
            UpdateResourceGenerator();
            
            if (enableDebugLog)
                logger.Log($"[{GetType().Name} - {Entity.Code}] Force multiplier changed to: {forceMultiplier}");
        }

        /// <summary>
        /// Sets the target ResourceGenerator at runtime.
        /// </summary>
        /// <param name="newTarget">New ResourceGenerator to control</param>
        public void SetTargetResourceGenerator(ResourceGenerator newTarget)
        {
            if (newTarget == null) return;

            // Restore previous target if exists
            RestoreOriginalValues();

            // Set new target
            targetResourceGenerator = newTarget;
            StoreOriginalValues();
            UpdateResourceGenerator();
            
            if (enableDebugLog)
                logger.Log($"[{GetType().Name} - {Entity.Code}] Target ResourceGenerator changed to: {newTarget.name}");
        }
        #endregion

        #region Editor Support
#if UNITY_EDITOR
        [Header("Workforce Debug Info (Runtime Only)")]
        [SerializeField, Tooltip("Current workforce count (runtime only)"), System.NonSerialized]
        private int debugWorkforceCount;
        
        [SerializeField, Tooltip("Current speed multiplier (runtime only)"), System.NonSerialized]
        private float debugSpeedMultiplier;
        
        [SerializeField, Tooltip("Current effective period (runtime only)"), System.NonSerialized]
        private float debugCurrentPeriod;

        private void Update()
        {
            // Update debug info in inspector during runtime
            if (Application.isPlaying)
            {
                debugWorkforceCount = WorkforceCount;
                debugSpeedMultiplier = CurrentSpeedMultiplier;
                debugCurrentPeriod = CurrentPeriod;
            }
        }

        private void OnValidate()
        {
            // Ensure force multiplier stays within reasonable bounds
            forceMultiplier = Mathf.Max(0.1f, forceMultiplier);
        }
#endif
        #endregion
    }
}