using UnityEngine;
using System.Reflection;
using RTSEngine.EntityComponent;
using RTSEngine.Event;

namespace DerekAssets.EntityComponent
{
    /// <summary>
    /// Modifies a ResourceGenerator based on units in a UnitCarrier.
    /// Simple connector component - no inheritance complexity.
    /// </summary>
    public class WorkforceResourceBooster : MonoBehaviour
    {
        [SerializeField] private UnitCarrier unitCarrier;
        [SerializeField] private ResourceGenerator resourceGenerator;
        [SerializeField, Range(0.1f, 3.0f)] private float speedMultiplierPerUnit = 1.0f;

        private FieldInfo periodField;
        private float originalPeriod;

        private void Start()
        {
            // Auto-find components if not assigned
            if (unitCarrier == null) unitCarrier = GetComponent<UnitCarrier>();
            if (resourceGenerator == null) resourceGenerator = GetComponent<ResourceGenerator>();

            if (unitCarrier == null || resourceGenerator == null)
            {
                Debug.LogError($"[{name}] Missing UnitCarrier or ResourceGenerator components!");
                return;
            }

            // Setup reflection to access ResourceGenerator's private period
            periodField = typeof(ResourceGenerator).GetField("period", BindingFlags.NonPublic | BindingFlags.Instance);
            if (periodField != null)
                originalPeriod = (float)periodField.GetValue(resourceGenerator);

            // Listen for workforce changes
            unitCarrier.UnitAdded += OnWorkforceChanged;
            unitCarrier.UnitRemoved += OnWorkforceChanged;

            // Apply initial state
            UpdateResourceGenerator();
        }

        private void OnDestroy()
        {
            if (unitCarrier != null)
            {
                unitCarrier.UnitAdded -= OnWorkforceChanged;
                unitCarrier.UnitRemoved -= OnWorkforceChanged;
            }
        }

        private void OnWorkforceChanged(IUnitCarrier sender, UnitCarrierEventArgs args)
        {
            UpdateResourceGenerator();
        }

        private void UpdateResourceGenerator()
        {
            if (periodField == null) return;

            int workerCount = unitCarrier.CurrAmount;
            bool hasWorkers = workerCount > 0;

            // Enable/disable based on workforce
            resourceGenerator.SetActiveLocal(hasWorkers, false);

            // Adjust speed: New Period = Original Period / (Multiplier * Workers)
            if (hasWorkers)
            {
                float newPeriod = originalPeriod / (speedMultiplierPerUnit * workerCount);
                periodField.SetValue(resourceGenerator, Mathf.Max(newPeriod, 0.1f));
            }
            else
            {
                periodField.SetValue(resourceGenerator, originalPeriod);
            }
        }
    }
}