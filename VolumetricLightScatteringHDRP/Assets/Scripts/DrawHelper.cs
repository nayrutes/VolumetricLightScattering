using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Visualisation;

[ExecuteInEditMode]
public class DrawHelper : MonoBehaviour
{

    public Froxels f;
    
    public float lineSize = 1;
    public float sphereSize = 1;
    public Vector3 offset;
    public GameObject camSymbol;
    
    public bool doDrawNow = false;
    public bool drawFrame = false;
    public bool drawAllLines = false;
    public bool drawCenters = false;
    
    // Start is called before the first frame update
    private Vector3 camSymOriginalPos;
    void Start()
    {
        camSymOriginalPos = camSymbol.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (f == null||f.pointsCamRelative == null || f.pointsCamRelative.Length == 0||f._froxelsCamRelative == null)
            return;
        if (doDrawNow)
        {
            DoDrawDepth();
            DrawCenters();
            //doDrawNow = false;
            //Vis.DrawSphere(new Vector3(0,0,0),size,Color.green,Style.Standard);
        }

        //camSymbol.transform.position = camSymOriginalPos + offset;
    }

    
    public void DoDrawDepth()
    {
        if (drawFrame)
        {
            DrawFrame();
        }

        if (drawAllLines)
        {
            DrawAllLines();
        }
    }

    void DrawCenters()
    {
        if(!drawCenters)
            return;
        Vector3Int amount = f.amount;
        Camera _camera = f._camera;
        //Vector3[] pointsCamRelative = f.pointsCamRelative;
        Froxels.Froxel[] froxels = f._froxelsCamRelative;
        if (froxels.Length < 4)
            return;
        
        Matrix4x4 comb = _camera.transform.localToWorldMatrix;
        for (int i = 0; i < froxels.Length; i++)
        {
            //z * ((amount.x) * (amount.y)) + y * (amount.x) + x
            //int x = froxels.Length % amount.x
            Vector3Int indexes = UnFlat(i, amount);
            Color c = GetColorOfPoint(indexes, amount);
            Vis.DrawSphere(froxels[i].center,sphereSize/25f,c,Style.Unlit);
            //GetColorOfPoint()
        }
    }
    
    void DrawFrame()
    {
        Vector3Int amount = f.amount;
        Camera _camera = f._camera;
        Vector3[] pointsCamRelative = f.pointsCamRelative;
        if (pointsCamRelative.Length < 4)
            return;
        
        Matrix4x4 comb = _camera.transform.localToWorldMatrix;
        for (int z = 0; z < amount.z + 1; z++)
        {
            DrawLeftToRight(0, z, pointsCamRelative, amount, comb);
            DrawLeftToRight(amount.y, z, pointsCamRelative, amount, comb);
            DrawBottomToTop(0, z, pointsCamRelative, amount, comb);
            DrawBottomToTop(amount.x, z, pointsCamRelative, amount, comb);
        }
    }


    void DrawAllLines()
    {
        Vector3Int amount = f.amount;
        Camera _camera = f._camera;
        Vector3[] pointsCamRelative = f.pointsCamRelative;
        if (pointsCamRelative.Length < 4)
            return;
        
        Matrix4x4 comb = _camera.transform.localToWorldMatrix;
        
        //draw fron to back
        for (int x = 0; x < amount.x+1; x++)
        {
            for (int y = 0; y < amount.y + 1; y++)
            {
                DrawFrontToBack(x,y,pointsCamRelative,amount,comb);
            }
        }
        //draw left to right
        for (int y = 0; y < amount.y+1; y++)
        {
            for (int z = 0; z < amount.z + 1; z++)
            {
                DrawLeftToRight(y,z,pointsCamRelative,amount,comb);
            }
        }
        //drawbottom to top
        for (int x = 0; x < amount.x+1; x++)
        {
            for (int z = 0; z < amount.z + 1; z++)
            {
                DrawBottomToTop(x,z,pointsCamRelative,amount,comb);
            }
        }
    }
    
    /// <summary>
    /// Index of Point
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    int GetIndex(int x, int y, int z, Vector3Int amount)
    {
        return z * ((amount.x + 1) * (amount.y + 1)) + y * (amount.x + 1) + x;
    }

    Color GetColorOfPoint(Vector3Int p, Vector3Int amount)
    {
        float r = p.x / (float) amount.x;
        float g = p.y / (float) amount.y;
        float b = p.z / (float) amount.z;
        Color c = new Color(r,g,b);
        return c;
    }
    
    void DrawLeftToRight(int y, int z, Vector3[] pointsCamRelative, Vector3Int amount, Matrix4x4 localToWorld)
    {
        Vector3 left = pointsCamRelative[GetIndex(0,y,z,amount)];
        Vector3 right = pointsCamRelative[GetIndex(amount.x,y,z,amount)];
        left = localToWorld.MultiplyPoint3x4(left);
        right = localToWorld.MultiplyPoint3x4(right);
        Vis.DrawLine(left,right, lineSize,Color.cyan, Style.Unlit);
    }
    
    void DrawBottomToTop(int x, int z, Vector3[] pointsCamRelative, Vector3Int amount, Matrix4x4 localToWorld)
    {
        Vector3 bottom = pointsCamRelative[GetIndex(x,0,z,amount)];
        Vector3 top = pointsCamRelative[GetIndex(x,amount.y,z,amount)];
        bottom = localToWorld.MultiplyPoint3x4(bottom);
        top = localToWorld.MultiplyPoint3x4(top);
        Vis.DrawLine(bottom,top, lineSize,Color.cyan, Style.Unlit);
    }
    
    void DrawFrontToBack(int x, int y, Vector3[] pointsCamRelative, Vector3Int amount, Matrix4x4 localToWorld)
    {
        Vector3 front = pointsCamRelative[GetIndex(x,y,0,amount)];
        Vector3 back = pointsCamRelative[GetIndex(x,y,amount.z,amount)];
        front = localToWorld.MultiplyPoint3x4(front);
        back = localToWorld.MultiplyPoint3x4(back);
        Vis.DrawLine(front,back, lineSize,Color.cyan, Style.Unlit);
    }
    
    Vector3Int UnFlat(int index, Vector3Int amount)
    {
        int x = (index % (amount.x*amount.y)%amount.x);
        int y = (index % (amount.x*amount.y)) / amount.x;
        int z = index / (amount.x * amount.y);
        return new Vector3Int(x,y,z);
    }
}
