using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        // MapGenerator mapGenerator = (MapGenerator)target;
        if (GUILayout.Button("Generate Map"))
        {
            // mapGenerator.GenerateMap();
            ((MapGenerator)target).GenerateMap();
        }
        if (GUILayout.Button("Clean Map"))
        {
            // mapGenerator.CleanMap();
            ((MapGenerator)target).CleanMap();
        }
    }

}
