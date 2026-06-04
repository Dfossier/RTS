using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ResourceGate))]
public class ResourceGateDrawer : PropertyDrawer
{
    private const float Padding    = 3f;
    private const float LineHeight = 18f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Let ResourceTypeInfo's own drawer dictate row 1's height (it includes a Search field).
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        float typeHeight = EditorGUI.GetPropertyHeight(typeProp, true);

        // Row 2 is always a single line (Min Level + Cost/Spawn).
        return typeHeight + LineHeight + Padding * 3;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty typeProp = property.FindPropertyRelative("type");
        SerializedProperty minProp  = property.FindPropertyRelative("minimumLevel");
        SerializedProperty costProp = property.FindPropertyRelative("costPerSpawn");

        float typeHeight = EditorGUI.GetPropertyHeight(typeProp, true);

        // Row 1 — resource type at whatever height its own drawer needs
        Rect row1 = new Rect(position.x, position.y + Padding, position.width, typeHeight);
        EditorGUI.PropertyField(row1, typeProp, new GUIContent("Resource Type"), true);

        // Row 2 — Min Level (left half) | Cost/Spawn (right half)
        float row2Y   = row1.y + typeHeight + Padding;
        float halfW   = position.width / 2f - 2f;
        Rect minRect  = new Rect(position.x,              row2Y, halfW, LineHeight);
        Rect costRect = new Rect(position.x + halfW + 4f, row2Y, halfW, LineHeight);

        float savedLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 72f;

        EditorGUI.PropertyField(minRect,  minProp,  new GUIContent("Min Level"));
        EditorGUI.PropertyField(costRect, costProp, new GUIContent("Cost/Spawn"));

        EditorGUIUtility.labelWidth = savedLabelWidth;

        EditorGUI.EndProperty();
    }
}
