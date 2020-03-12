﻿using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[ExecuteInEditMode]
public class ProjectionTest : MonoBehaviour
{
    private Camera cam;
    [SerializeField] bool normalizedBox;

    [Header("!!!! Only z-Tested !!!!")]
    [SerializeField] private Vector3 pointInScene;
    [SerializeField] private Vector3 pointInBox;

    [SerializeField] private bool drawFrustumToBox;
    [SerializeField] private bool drawBoxToFrustum;
    [SerializeField] private bool alignInBoxXY;
    [SerializeField] private bool drawDistance;

    private Vector4 projectedIntoBoxLocal;

    private Vector3 projectedIntoFrustumWorld;

    private bool distancePositive;
    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        DrawDebugCube();
        TestProjectionFromWorldToBox();
        if (alignInBoxXY)
        {
            pointInBox = cam.transform.worldToLocalMatrix * pointInBox;
            pointInBox.x = projectedIntoBoxLocal.x;
            pointInBox.y = projectedIntoBoxLocal.y;
            pointInBox = cam.transform.localToWorldMatrix * pointInBox;
        }
        TestProjectionFromBoxToWorld();
        TestPositiveDistance();
        if (drawDistance)
        {
            Color c;
            if (distancePositive)
                c = Color.blue;
            else
                c = Color.red;
                
            //Different if projectedIntoBoxLocal is Vector3
            Debug.DrawLine(cam.transform.localToWorldMatrix * projectedIntoBoxLocal,pointInBox,c);
            Debug.DrawLine(projectedIntoFrustumWorld, pointInScene, c);
        }
    }
    
    public void TestProjectionFromWorldToBox()
    {
        //Point from World to Box
        Matrix4x4 camP = cam.projectionMatrix;
        Vector4 pointInScene = this.pointInScene;
        //Add w to point for projection
        pointInScene.w = 1;
        Matrix4x4 scaleMatrix = Matrix4x4.identity;
        scaleMatrix.m22 = -1.0f;
        Matrix4x4 v = scaleMatrix * cam.transform.worldToLocalMatrix;
        Matrix4x4 vp = camP * v;
        //get point projected in localSpace
        Vector4 pointProjected = vp * pointInScene;
        //correct projection by depthfactor -> now in box
        pointProjected /= pointProjected.w;
        Color cResult;
        //!!!! Only z-Tested !!!!
        if (pointProjected.z > 1 || pointProjected.z < -1)
        {
            cResult = Color.red;
        }
        else
        {
            cResult = Color.green;
        }

        if (normalizedBox)
        {
            Matrix4x4 converter = createConvertionMatrixMinus11To01();
            pointProjected = converter *pointProjected; // same as MultiplyPoint3x4, ignore scaling
        }
        projectedIntoBoxLocal = pointProjected;
        Vector3 pointProjectedInWorld = cam.transform.localToWorldMatrix * pointProjected;
        if (drawFrustumToBox)
        {
            Froxels.DrawPointCross(pointInScene,0.2f,Color.cyan);
            Froxels.DrawPointCross(pointProjectedInWorld,0.2f,cResult);
        }
    }

    public void TestProjectionFromBoxToWorld()
    {
        Vector4 pointInBox = this.pointInBox;
        pointInBox.w = 1;
        Matrix4x4 camP = cam.projectionMatrix;
        Matrix4x4 scale = Matrix4x4.identity;
        scale.m22 = -1;
        Matrix4x4 v = scale * cam.transform.worldToLocalMatrix;
        Matrix4x4 vp = camP * v;
        Matrix4x4 inverseVp = vp.inverse;

        //get point local in Box
        Vector4 pointInBoxLocal = cam.transform.worldToLocalMatrix * pointInBox;
        //project point from box to frustum in World
        Vector4 ponintInFrustum = inverseVp * pointInBoxLocal;
        //correct depth (and other sides because we are already in world space)
        ponintInFrustum /= ponintInFrustum.w;

        projectedIntoFrustumWorld = ponintInFrustum;
        if (drawBoxToFrustum)
        {
            Froxels.DrawPointCross(pointInBox, 0.2f,Color.magenta);
            Froxels.DrawPointCross(ponintInFrustum,0.2f,Color.yellow);
        }
    }
    void TestPositiveDistance()
    {
        distancePositive = cam.transform.worldToLocalMatrix.MultiplyPoint3x4(pointInBox).z >
                           projectedIntoBoxLocal.z;
    }

    private void DrawDebugCube()
    {
        //lbf, rbf, ltf, rtf, lbn, rbn, ltn, rtn,
        Vector3[] p = new Vector3[]
        {
            new Vector3(-1, -1, -1),
            new Vector3(1, -1, -1),
            new Vector3(-1, 1, -1),
            new Vector3(1, 1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(1, -1, 1),
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1)
        };

        if (normalizedBox)
        {
            Matrix4x4 c = createConvertionMatrixMinus11To01();
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = c.MultiplyPoint3x4(p[i]);
            }
        }
        
        for (var i = 0; i < p.Length; i++)
        {
            p[i] = cam.transform.localToWorldMatrix.MultiplyPoint3x4(p[i]);
        }

        //front
        QuickDrawLine(p,0,1);
        QuickDrawLine(p,2,3);
        QuickDrawLine(p,0,2);
        QuickDrawLine(p,1,3);
        //back
        QuickDrawLine(p,0,4);
        QuickDrawLine(p,1,5);
        QuickDrawLine(p,2,6);
        QuickDrawLine(p,3,7);
        //conection
        QuickDrawLine(p,4,5);
        QuickDrawLine(p,6,7);
        QuickDrawLine(p,4,6);
        QuickDrawLine(p,5,7);
    }

    private void QuickDrawLine(Vector3[] points, int p1, int p2)
    {
        Debug.DrawLine(points[p1],points[p2],Color.white);
    }

    private Matrix4x4 createConvertionMatrixMinus11To01()
    {
        Matrix4x4 c = new Matrix4x4
        (new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
            new Vector4(0.5f, 0.5f, 0.5f, 1.0f)
        );
        return c;
    }
}
