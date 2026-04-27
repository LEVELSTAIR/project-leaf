using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TreeGrassTerrainArea))]
public class TreeGrassTerrainAreaEditor : Editor
{
    private SerializedProperty detailPrototypeIndex;
    private Terrain terrain;

    private void OnEnable()
    {
        detailPrototypeIndex = serializedObject.FindProperty("detailPrototypeIndex");
        terrain = Terrain.activeTerrain;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw all other properties normally
        DrawPropertiesExcluding(serializedObject, "detailPrototypeIndex");

        // Draw a dropdown for the grass type
        if (terrain != null && terrain.terrainData != null)
        {
            var prototypes = terrain.terrainData.detailPrototypes;
            string[] names = new string[prototypes.Length];
            for (int i = 0; i < prototypes.Length; i++)
            {
                // Use the prototype's texture name or just a generic name
                names[i] = prototypes[i].prototypeTexture != null
                    ? prototypes[i].prototypeTexture.name
                    : $"Grass {i}";
            }

            int currentIndex = detailPrototypeIndex.intValue;
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Detail Prototype", currentIndex, names);
            if (EditorGUI.EndChangeCheck())
            {
                detailPrototypeIndex.intValue = Mathf.Clamp(newIndex, 0, prototypes.Length - 1);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No active Terrain found. Please assign a Terrain to the scene.", MessageType.Warning);
            EditorGUILayout.PropertyField(detailPrototypeIndex, new GUIContent("Detail Prototype Index (int)"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}