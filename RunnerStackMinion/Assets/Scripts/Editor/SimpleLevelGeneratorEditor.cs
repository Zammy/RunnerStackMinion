using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleLevelGenerator))]
public class SimpleLevelGeneratorEditor : Editor
{
    static int sSelectedLevel = 0;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (Application.isPlaying)
            return;

        var levelGenerator = (SimpleLevelGenerator)target;

        EditorGUILayout.Space(20f);
        GUILayout.Label("Level Editor");

        EditorGUILayout.BeginVertical();
        sSelectedLevel = EditorGUILayout.IntField("Selected Level", sSelectedLevel);

        for (int i = 0; i < levelGenerator.Segments.Length; i++)
        {
            var segment = levelGenerator.Segments[i];
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.TextField(segment.Prefab.name);
            GUI.enabled = true;
            if (GUILayout.Button("Spawn"))
            {
                levelGenerator.AddSegmentIndexToLevel(i, sSelectedLevel);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }
}
