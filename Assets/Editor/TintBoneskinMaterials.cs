#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TintBoneskinMaterials
{
    [MenuItem("Tools/Tint Boneskin Materials")]
    static void Apply()
    {
        string folder = "Assets/Boneskin settlement pack/Models/Materials";
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                // Shift toward warm: boost red slightly, reduce blue
                c.r = Mathf.Min(1f, c.r * 1.08f);
                c.g = Mathf.Min(1f, c.g * 1.02f);
                c.b = Mathf.Max(0f, c.b * 0.88f);
                mat.SetColor("_Color", c);
                EditorUtility.SetDirty(mat);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TintBoneskin] Warmed {count} materials.");
    }
}
#endif
