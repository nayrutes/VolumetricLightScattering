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
    
    public float size = 1;
    public Vector3 offset;
    public GameObject camSymbol;
    
    public bool doDrawNow = false;
    public bool drawFrame = false;
    public bool drawAllLines = false;
    
    // Start is called before the first frame update
    private Vector3 camSymOriginalPos;
    void Start()
    {
        camSymOriginalPos = camSymbol.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (doDrawNow)
        {
            DoDrawDepth();
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
    
    void DrawFrame()
    {
        Vector3Int amount = f.amount;
        Camera _camera = f._camera;
        Vector3[] pointsCamRelative = f.pointsCamRelative;
        if (pointsCamRelative.Length < 4)
            return;
        
        //z
        for (int z = 0; z < amount.z+1; z++)
        {
            Matrix4x4 comb = _camera.transform.localToWorldMatrix;            

            Vector3 bottomleft = pointsCamRelative[GetIndex(z,0,0,amount)];//lbn
            Vector3 topleft = pointsCamRelative[GetIndex(z,amount.y,0,amount)];//ltn z y 0
            Vector3 bottomright = pointsCamRelative[GetIndex(z,0,amount.x,amount)];//lbn z 0 x
            Vector3 topright = pointsCamRelative[GetIndex(z,amount.y,amount.x,amount)];//ltn z y x
            bottomleft = comb.MultiplyPoint3x4(bottomleft);
            topleft = comb.MultiplyPoint3x4(topleft);
            bottomright = comb.MultiplyPoint3x4(bottomright);
            topright = comb.MultiplyPoint3x4(topright);
            Vis.DrawLine(bottomleft,topleft, size,Color.cyan, Style.Unlit);
            Vis.DrawLine(topright,bottomright, size,Color.cyan, Style.Unlit);
            Vis.DrawLine(bottomleft,bottomright, size,Color.cyan, Style.Unlit);
            Vis.DrawLine(topright,topleft, size,Color.cyan, Style.Unlit);
            
        }
        
        //y
        for (int y = 0; y < amount.y+1; y++)
        {
            Matrix4x4 comb = _camera.transform.localToWorldMatrix;            

            Vector3 leftNear = pointsCamRelative[GetIndex(0,y,0,amount)];
            Vector3 leftFar = pointsCamRelative[GetIndex(0,y,amount.z,amount)];
            Vector3 rightNear = pointsCamRelative[GetIndex(amount.x,y,0,amount)];
            Vector3 rightFar = pointsCamRelative[GetIndex(amount.x,y,amount.z,amount)];
            leftNear = comb.MultiplyPoint3x4(leftNear);
            leftFar = comb.MultiplyPoint3x4(leftFar);
            rightNear = comb.MultiplyPoint3x4(rightNear);
            rightFar = comb.MultiplyPoint3x4(rightFar);
            Vis.DrawLine(leftNear,leftFar, size,Color.cyan, Style.Unlit);
            Vis.DrawLine(leftNear,rightNear, size,Color.cyan, Style.Unlit);
            Vis.DrawLine(rightFar,rightNear, size,Color.cyan, Style.Unlit);
            Vis.DrawLine(rightFar,leftFar, size,Color.cyan, Style.Unlit);
            
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

    void DrawLeftToRight(int y, int z, Vector3[] pointsCamRelative, Vector3Int amount, Matrix4x4 localToWorld)
    {
        Vector3 left = pointsCamRelative[GetIndex(0,y,z,amount)];
        Vector3 right = pointsCamRelative[GetIndex(amount.x,y,z,amount)];
        left = localToWorld.MultiplyPoint3x4(left);
        right = localToWorld.MultiplyPoint3x4(right);
        Vis.DrawLine(left,right, size,Color.cyan, Style.Unlit);
    }
    
    void DrawBottomToTop(int x, int z, Vector3[] pointsCamRelative, Vector3Int amount, Matrix4x4 localToWorld)
    {
        Vector3 bottom = pointsCamRelative[GetIndex(x,0,z,amount)];
        Vector3 top = pointsCamRelative[GetIndex(x,amount.y,z,amount)];
        bottom = localToWorld.MultiplyPoint3x4(bottom);
        top = localToWorld.MultiplyPoint3x4(top);
        Vis.DrawLine(bottom,top, size,Color.cyan, Style.Unlit);
    }
    
    void DrawFrontToBack(int x, int y, Vector3[] pointsCamRelative, Vector3Int amount, Matrix4x4 localToWorld)
    {
        Vector3 front = pointsCamRelative[GetIndex(x,y,0,amount)];
        Vector3 back = pointsCamRelative[GetIndex(x,y,amount.z,amount)];
        front = localToWorld.MultiplyPoint3x4(front);
        back = localToWorld.MultiplyPoint3x4(back);
        Vis.DrawLine(front,back, size,Color.cyan, Style.Unlit);
    }
}
