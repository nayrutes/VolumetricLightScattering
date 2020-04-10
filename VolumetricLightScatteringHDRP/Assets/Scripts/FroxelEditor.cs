using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Froxels))]
public class FroxelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Froxels froxels = (Froxels) target;
        if (GUILayout.Button("Generate Froxels"))
        {
            froxels.GenerateFroxelsInCameraSpace();
        }
        bool generated = froxels.pointsCamRelative != null && froxels.pointsCamRelative.Length > 0;
        EditorGUILayout.LabelField("Froxels generated: ",generated? "yes": "no");
        GUILayout.Space(10);
        base.OnInspectorGUI();
    }
}
