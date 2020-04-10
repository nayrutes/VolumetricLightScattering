using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityTemplateProjects;

public class CameraSetups : MonoBehaviour
{
    public GameObject mainCamera;
    public List<Trans2> setupPoints = new List<Trans2>();
    [HideInInspector] public int currentIndex = 0;
    
    public void CreatePoint(int index)
    {
        setupPoints.Insert(index, mainCamera.transform.SaveTrans());
    }
    public void RemovePoint(int index)
    {
        setupPoints.RemoveAt(index);
    }
    public void SetToIndex(int index)
    {
        SimpleCameraController scc = mainCamera.GetComponent<SimpleCameraController>();
        bool sccB = false;
        if (scc != null)
        {
            sccB = scc.enabled;
            scc.enabled = false;
        }
        mainCamera.transform.LoadTrans(setupPoints[index]);
        if (scc != null)
        {
            scc.enabled = sccB;
        }
    }
}
//https://answers.unity.com/questions/1296012/best-way-to-set-game-object-transforms.html
[Serializable]
public class Trans2
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;
 
    public Trans2(Transform trans)
    {
        this.position = trans.position;
        this.rotation = trans.rotation;
        this.localScale = trans.localScale;
    }
}

public static class TransformExtention{
    public static void LoadTrans(this Transform original, Trans2 savedCopy)
    {
        original.position   = savedCopy.position;
        original.rotation   = savedCopy.rotation;
        original.localScale = savedCopy.localScale;
    }
    public static Trans2 SaveTrans(this Transform original)
    {
        return new Trans2(original);
    }
}

[CustomEditor(typeof(CameraSetups))]
public class CameraSetupsEditor : Editor
{
    //private int index;
    public override void OnInspectorGUI()
    {
        CameraSetups camSet = (CameraSetups) target;
        
        base.OnInspectorGUI();
        EditorGUILayout.LabelField("Current Index: "+camSet.currentIndex);
        if (GUILayout.Button("Create Point From Current"))
        {
            camSet.currentIndex++;
            camSet.CreatePoint(camSet.currentIndex);
        }
        if (GUILayout.Button("Remove at current Index"))
        {
            camSet.RemovePoint(camSet.currentIndex);
            camSet.currentIndex = (camSet.currentIndex - 1 + camSet.setupPoints.Count) % camSet.setupPoints.Count;
            camSet.SetToIndex(camSet.currentIndex);
        }
        if (GUILayout.Button("Go To Next"))
        {
            camSet.currentIndex = (camSet.currentIndex + 1) % camSet.setupPoints.Count;
            camSet.SetToIndex(camSet.currentIndex);
        }
        if (GUILayout.Button("Go To Prev"))
        {
            camSet.currentIndex = (camSet.currentIndex - 1 + camSet.setupPoints.Count) % camSet.setupPoints.Count;
            camSet.SetToIndex(camSet.currentIndex);
        }
    }
}