using RTSEngine.EntityComponent;
using RTSEngine.Utilities;
using UnityEditor;
using UnityEngine;

namespace RTSEngine.EditorOnly.EntityComponent
{
    [CustomEditor(typeof(ResourceGeneratorToggleUI))]
    public class ResourceGeneratorToggleUIEditor : TabsEditorBase<ResourceGeneratorToggleUI>
    {
        protected override Int2D tabID
        {
            get => comp.tabID;
            set => comp.tabID = value;
        }

        private string[][] toolbars = new string[][] {
            new string [] { "General", "Task UI" },
        };

        public override void OnInspectorGUI()
        {
            OnInspectorGUI(toolbars);
        }

        protected override void OnTabSwitch(string tabName)
        {
            switch (tabName)
            {
                case "General":
                    OnGeneralInspectorGUI();
                    break;
                case "Task UI":
                    OnTaskUIInspectorGUI();
                    break;
            }
        }

        protected virtual void OnGeneralInspectorGUI()
        {
            EditorGUILayout.PropertyField(SO.FindProperty("code"));
            EditorGUILayout.PropertyField(SO.FindProperty("isActive"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Generator Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(SO.FindProperty("targetGenerator"));
            EditorGUILayout.PropertyField(SO.FindProperty("startEnabled"));
        }

        protected virtual void OnTaskUIInspectorGUI()
        {
            EditorGUILayout.LabelField("Task UI Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create one EntityComponentTaskUI asset and choose how to show on/off state.",
                MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(SO.FindProperty("toggleTask"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visual Effect", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(SO.FindProperty("effectMode"));

            var effectMode = SO.FindProperty("effectMode").enumValueIndex;

            EditorGUILayout.Space();

            switch (effectMode)
            {
                case 0: // IconSwap
                    EditorGUILayout.HelpBox("Icon Swap: Button icon changes based on state.\n" +
                        "• Active Icon: Shows when generation is ON\n" +
                        "• Inactive Icon: Shows when generation is OFF",
                        MessageType.Info);
                    EditorGUILayout.PropertyField(SO.FindProperty("activeIcon"));
                    EditorGUILayout.PropertyField(SO.FindProperty("inactiveIcon"));
                    break;

                case 1: // ColorTint
                    EditorGUILayout.HelpBox("Color Tint: Tooltip text color changes based on state.\n" +
                        "• Active Color: Tint when generation is ON (default: green)\n" +
                        "• Inactive Color: Tint when generation is OFF (default: red)",
                        MessageType.Info);
                    EditorGUILayout.PropertyField(SO.FindProperty("activeColor"));
                    EditorGUILayout.PropertyField(SO.FindProperty("inactiveColor"));
                    break;

                case 2: // LockedState
                    EditorGUILayout.HelpBox("Locked State: Button appears grayed/locked when OFF, normal when ON.\n" +
                        "Uses RTS Engine's built-in locked visual state.\n" +
                        "No additional setup required!",
                        MessageType.Info);
                    break;
            }
        }
    }
}
