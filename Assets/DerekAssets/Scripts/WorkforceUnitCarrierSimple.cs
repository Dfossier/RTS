using UnityEngine;
using System.Reflection;
using RTSEngine.EntityComponent;
using RTSEngine.Event;

namespace DerekAssets.EntityComponent
{
    /// <summary>
    /// Simple extension of UnitCarrier that modifies a ResourceGenerator based on carried units.
    /// Acts as a clean interface between the two components.
    /// </summary>
    public class WorkforceUnitCarrierSimple : UnitCarrier
    {
        [Header("Workforce Settings")]
        [SerializeField, Tooltip("The ResourceGenerator to control.")]
        private ResourceGenerator targetResourceGenerator;

        [SerializeField, Tooltip("Speed multiplier per unit (1.0 = 100% boost per unit)."), Range(0.1f, 3.0f)]
        private float forceMultiplier = 1.0f;

        // Reflection access to ResourceGenerator's private period field
        private FieldInfo periodField;
        private float originalPeriod;

        protected override void OnInit()
        {
            base.OnInit();

            // Find ResourceGenerator if not assigned
            if (targetResourceGenerator == null)
                targetResourceGenerator = Entity.GetComponent<ResourceGenerator>();

            if (targetResourceGenerator == null)
            {
                logger.LogError($"[{GetType().Name}] No ResourceGenerator found!");
                return;
            }

            // Setup reflection and store original period
            periodField = typeof(ResourceGenerator).GetField("period", BindingFlags.NonPublic | BindingFlags.Instance);
            if (periodField != null)
                originalPeriod = (float)periodField.GetValue(targetResourceGenerator);

            // Subscribe to events and apply initial state
            UnitAdded += OnUnitChanged;
            UnitRemoved += OnUnitChanged;
            UpdateResourceGenerator();
        }

        protected override void OnDisabled()
        {
            UnitAdded -= OnUnitChanged;
            UnitRemoved -= OnUnitChanged;
            
            // Restore original state
            if (targetResourceGenerator != null)
            {
                targetResourceGenerator.SetActiveLocal(true, false);
                if (periodField != null)
                    periodField.SetValue(targetResourceGenerator, originalPeriod);
            }

            base.OnDisabled();
        }

        private void OnUnitChanged(IUnitCarrier sender, UnitCarrierEventArgs args)
        {
            UpdateResourceGenerator();
        }

        private void UpdateResourceGenerator()
        {
            if (targetResourceGenerator == null || periodField == null)
                return;

            bool hasWorkers = CurrAmount > 0;
            
            // Enable/disable generator based on workforce
            targetResourceGenerator.SetActiveLocal(hasWorkers, false);

            // Adjust speed based on workforce
            if (hasWorkers)
            {
                float newPeriod = originalPeriod / (forceMultiplier * CurrAmount);
                periodField.SetValue(targetResourceGenerator, Mathf.Max(newPeriod, 0.1f));
            }
            else
            {
                periodField.SetValue(targetResourceGenerator, originalPeriod);
            }
        }
    }
}