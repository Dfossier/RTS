using UnityEngine;
using UnityEditor;
using DerekAssets.EntityComponent;
using RTSEngine.EntityComponent;

namespace DerekAssets.Editor
{
    [CustomEditor(typeof(WorkforceUnitCarrier))]
    public class WorkforceUnitCarrierEditor : UnityEditor.Editor
    {
        private WorkforceUnitCarrier workforceCarrier;
        private SerializedProperty targetResourceGeneratorProp;
        private SerializedProperty forceMultiplierProp;
        private SerializedProperty enableDebugLogProp;

        private bool showWorkforceInfo = true;
        private bool showCalculations = true;

        private void OnEnable()
        {
            workforceCarrier = (WorkforceUnitCarrier)target;
            
            targetResourceGeneratorProp = serializedObject.FindProperty("targetResourceGenerator");
            forceMultiplierProp = serializedObject.FindProperty("forceMultiplier");
            enableDebugLogProp = serializedObject.FindProperty("enableDebugLog");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw default UnitCarrier inspector first
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            
            // Custom Workforce section
            EditorGUILayout.LabelField("Workforce Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(targetResourceGeneratorProp, new GUIContent("Target Resource Generator", "The ResourceGenerator to control based on workforce"));
            EditorGUILayout.PropertyField(forceMultiplierProp, new GUIContent("Force Multiplier", "Multiplier per unit for generation speed (1.0 = 100% per unit)"));
            EditorGUILayout.PropertyField(enableDebugLogProp, new GUIContent("Enable Debug Logging", "Log workforce calculations to console"));

            EditorGUILayout.Space(5);

            // Auto-find ResourceGenerator button
            if (GUILayout.Button("Auto-Find ResourceGenerator on Entity"))
            {
                ResourceGenerator foundGenerator = workforceCarrier.Entity?.GetComponent<ResourceGenerator>();
                if (foundGenerator != null)
                {
                    targetResourceGeneratorProp.objectReferenceValue = foundGenerator;
                    EditorUtility.SetDirty(target);
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", "No ResourceGenerator component found on the same entity.", "OK");
                }
            }

            EditorGUILayout.Space(10);

            // Runtime information
            if (Application.isPlaying && workforceCarrier != null)
            {
                EditorGUILayout.LabelField("Runtime Workforce Information", EditorStyles.boldLabel);
                
                showWorkforceInfo = EditorGUILayout.Foldout(showWorkforceInfo, "Workforce Status", true);
                if (showWorkforceInfo)
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.LabelField($"Workforce Count: {workforceCarrier.WorkforceCount} / {workforceCarrier.MaxAmount}");
                    EditorGUILayout.LabelField($"Speed Multiplier: {workforceCarrier.CurrentSpeedMultiplier:F2}x");
                    
                    bool isGeneratorActive = workforceCarrier.WorkforceCount > 0;
                    EditorGUILayout.LabelField($"Generator Active: {isGeneratorActive}");
                    
                    if (isGeneratorActive)
                    {
                        EditorGUILayout.LabelField($"Current Period: {workforceCarrier.CurrentPeriod:F2}s");
                    }
                    
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(5);

                showCalculations = EditorGUILayout.Foldout(showCalculations, "Calculations Preview", true);
                if (showCalculations)
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.LabelField("Formula: New Period = Original Period / (Force Multiplier × Unit Count)");
                    
                    if (targetResourceGeneratorProp.objectReferenceValue != null)
                    {
                        // Show calculations for different unit counts
                        for (int units = 1; units <= Mathf.Min(workforceCarrier.MaxAmount, 5); units++)
                        {
                            float multiplier = forceMultiplierProp.floatValue * units;
                            float theoreticalPeriod = Mathf.Max(1.0f / multiplier, 0.1f); // Assuming 1.0s original period for preview
                            EditorGUILayout.LabelField($"{units} unit(s): {multiplier:F2}x speed ({theoreticalPeriod:F2}s period)");
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(5);

                // Runtime controls
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Update Generator"))
                {
                    // Use reflection to call the private UpdateResourceGenerator method
                    var method = typeof(WorkforceUnitCarrier).GetMethod("UpdateResourceGenerator", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(workforceCarrier, null);
                }
                
                if (GUILayout.Button("Log Current State"))
                {
                    Debug.Log($"[WorkforceUnitCarrier - {workforceCarrier.Entity?.Code}] " +
                        $"Workforce: {workforceCarrier.WorkforceCount}/{workforceCarrier.MaxAmount}, " +
                        $"Speed: {workforceCarrier.CurrentSpeedMultiplier:F2}x, " +
                        $"Period: {workforceCarrier.CurrentPeriod:F2}s");
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (!Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("Runtime information will be displayed when the game is playing.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}