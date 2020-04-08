using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using Visualisation;

[ExecuteInEditMode]
public class ProjectionTest : MonoBehaviour
{
    private Camera cam;
    [SerializeField] bool normalizedBox;

     
    [SerializeField] private GameObject camSymbol;
    [SerializeField] private GameObject camLookPoint;
    [SerializeField] private List<float> samples = new List<float>();
    [SerializeField] private int selectedSample;
    [SerializeField] private Vector3 pointInBox;
    [Range(0.0f,1.0f)]
    [SerializeField] private float distance;
    
    [SerializeField] private bool drawFrustumToBox;
    [SerializeField] private bool drawBoxToFrustum;
    [SerializeField] private bool alignInBoxXY;
    [SerializeField] private bool drawDistance;

    [Header("Spheres")]
    [SerializeField] private Color c2;
    [SerializeField] private Color c3;
    [Header("Lines")]
    [SerializeField] private Color c1;
    [SerializeField] private Color c4;
    [SerializeField] private Color c5;
    [SerializeField] private Color c6;

    [SerializeField] private float sphereSize;
    [SerializeField] private float lineSize;
    
    private Vector3 pointInScene;
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
        DrawSamples();
        Vector3 dir = camLookPoint.transform.position - camSymbol.transform.position;
        dir = dir.normalized;
        pointInScene = camSymbol.transform.position+dir*samples[selectedSample];
        Vis.DrawLine(camSymbol.transform.position,camLookPoint.transform.position,lineSize,c6,Style.Unlit);
        
        DrawDebugCube(cam.transform, normalizedBox, c1, lineSize);
        DrawFrustum(cam, c1, lineSize);
        TestProjectionFromWorldToBox(c2);
        if (alignInBoxXY)
        {
            pointInBox = cam.transform.worldToLocalMatrix * pointInBox;
            pointInBox.x = projectedIntoBoxLocal.x;
            pointInBox.y = projectedIntoBoxLocal.y;
            pointInBox = cam.transform.localToWorldMatrix * pointInBox;
        }
        
        projectedIntoFrustumWorld = TestProjectionFromBoxToWorld(pointInBox,cam);
        if (drawBoxToFrustum)
        {
            //Froxels.DrawPointCross(pointInBox, 0.2f,Color.magenta);
            //Froxels.DrawPointCross(ponintInFrustum,0.2f,Color.yellow);
            Vis.DrawSphere(pointInBox,sphereSize, c3, Style.Unlit);
            Vis.DrawSphere(projectedIntoFrustumWorld,sphereSize, c3, Style.Unlit);
        }
        
        
        TestPositiveDistance();
        if (drawDistance)
        {
            Color c;
            if (distancePositive)
                c = Color.red;
            else
                c = c4;
                
            //Different if projectedIntoBoxLocal is Vector3
            //Debug.DrawLine(cam.transform.localToWorldMatrix * projectedIntoBoxLocal,pointInBox,c);
            //Debug.DrawLine(projectedIntoFrustumWorld, pointInScene, c);
            Vis.DrawLine(cam.transform.localToWorldMatrix * projectedIntoBoxLocal,pointInBox,lineSize,c,Style.Unlit);
            Vis.DrawLine(projectedIntoFrustumWorld,pointInScene,lineSize,c,Style.Unlit);
            
            //Draw line to near
            Vector3 pointOnNear = cam.transform.worldToLocalMatrix*pointInBox;
            pointOnNear.z = -1f;
            pointOnNear = cam.transform.localToWorldMatrix * pointOnNear;
            pointOnNear = TestProjectionFromBoxToWorld(pointOnNear,cam);
            Vis.DrawLine(projectedIntoFrustumWorld, pointOnNear, lineSize, c5, Style.Unlit);

        }
        DrawByDistance(new Vector2(projectedIntoBoxLocal.x,projectedIntoBoxLocal.y),distance);
    }
    
    public void TestProjectionFromWorldToBox(Color c)
    {
//        //Point from World to Box
//        Matrix4x4 camP = cam.projectionMatrix;
//        Vector4 pointInScene = this.pointInScene;
//        //Add w to point for projection
//        pointInScene.w = 1;
//        Matrix4x4 scaleMatrix = Matrix4x4.identity;
//        scaleMatrix.m22 = -1.0f;
//        Matrix4x4 v = scaleMatrix * cam.transform.worldToLocalMatrix;
//        Matrix4x4 vp = camP * v;
//        //get point projected in localSpace
//        Vector4 pointProjected = vp * pointInScene;
//        //correct projection by depthfactor -> now in box
//        pointProjected /= pointProjected.w;
//        Color cResult;
//        //!!!! Only z-Tested !!!!
//        if (pointProjected.z > 1 || pointProjected.z < -1)
//        {
//            cResult = Color.red;
//        }
//        else
//        {
//            cResult = Color.green;
//        }
//
//        if (normalizedBox)
//        {
//            Matrix4x4 converter = CreateConvertionMatrixMinus11To01();
//            pointProjected = converter *pointProjected; // same as MultiplyPoint3x4, ignore scaling
//        }
//        projectedIntoBoxLocal = pointProjected;
//        Vector3 pointProjectedInWorld = cam.transform.localToWorldMatrix * pointProjected;
        bool isInside;
        projectedIntoBoxLocal = WorldToProjectedLocal(pointInScene, cam, normalizedBox, out isInside);
        Vector3 pointProjectedInWorld = ToWorld(projectedIntoBoxLocal, cam.transform);
        Color cResult = isInside ? c : Color.red;
        if (drawFrustumToBox)
        {
            //Froxels.DrawPointCross(pointInScene,0.2f,Color.cyan);
            //Froxels.DrawPointCross(pointProjectedInWorld,0.2f,cResult);
            Vis.DrawSphere(pointInScene,sphereSize,c,Style.Unlit);
            Vis.DrawSphere(pointProjectedInWorld,sphereSize,cResult,Style.Unlit);
        }
    }

    public static Vector3 TestProjectionFromBoxToWorld(Vector3 pointInBox, Camera cam)
    {
        Vector4 pointInBoxV4 = pointInBox;
        pointInBoxV4.w = 1;
        Matrix4x4 camP = cam.projectionMatrix;
        Matrix4x4 scale = Matrix4x4.identity;
        scale.m22 = -1;
        Matrix4x4 v = scale * cam.transform.worldToLocalMatrix;
        Matrix4x4 vp = camP * v;
        Matrix4x4 inverseVp = vp.inverse;

        //get point local in Box
        Vector4 pointInBoxLocal = cam.transform.worldToLocalMatrix * pointInBoxV4;
        //project point from box to frustum in World
        Vector4 ponintInFrustum = inverseVp * pointInBoxLocal;
        //correct depth (and other sides because we are already in world space)
        ponintInFrustum /= ponintInFrustum.w;

        return new Vector3(ponintInFrustum.x,ponintInFrustum.y,ponintInFrustum.z);
    }
    void TestPositiveDistance()
    {
        distancePositive = cam.transform.worldToLocalMatrix.MultiplyPoint3x4(pointInBox).z >
                           projectedIntoBoxLocal.z;
    }

    void DrawByDistance(Vector2 line, float d)
    {
        Vector3 inBox = new Vector3(line.x,line.y,d);
        //inBox = createConvertionMatrixMinus11To01() * inBox;
        Vector3 inBoxWorld = cam.transform.localToWorldMatrix.MultiplyPoint(inBox);
        //Froxels.DrawPointCross(inBoxWorld,0.2f,Color.grey);
        //Vis.DrawSphere(inBoxWorld,0.2f,Color.grey,Style.Unlit);
        if (projectedIntoBoxLocal.z > d)
        {
            Debug.DrawLine(inBoxWorld,cam.transform.localToWorldMatrix * projectedIntoBoxLocal,Color.red);
        }
        else
        {
            Debug.DrawLine(inBoxWorld,cam.transform.localToWorldMatrix * projectedIntoBoxLocal,Color.green);
        }
    }
    
    public static void DrawDebugCube(Transform t, bool normalizedTo01, Color color, float lineSize = 0.3f)
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

        if (normalizedTo01)
        {
            Matrix4x4 c = CreateConvertionMatrixMinus11To01();
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = c.MultiplyPoint3x4(p[i]);
            }
        }
        
        for (var i = 0; i < p.Length; i++)
        {
            p[i] = t.localToWorldMatrix.MultiplyPoint3x4(p[i]);
        }
        
        //front
        QuickDrawLine(p,0,1, color, lineSize);
        QuickDrawLine(p,2,3, color, lineSize);
        QuickDrawLine(p,0,2, color, lineSize);
        QuickDrawLine(p,1,3, color, lineSize);
        //back
        QuickDrawLine(p,0,4, color, lineSize);
        QuickDrawLine(p,1,5, color, lineSize);
        QuickDrawLine(p,2,6, color, lineSize);
        QuickDrawLine(p,3,7, color, lineSize);
        //conection
        QuickDrawLine(p,4,5, color, lineSize);
        QuickDrawLine(p,6,7, color, lineSize);
        QuickDrawLine(p,4,6, color, lineSize);
        QuickDrawLine(p,5,7, color, lineSize);
    }

    public static void DrawFrustum(Camera cam, Color c, float thickness = 0.3f)
    {
        Vector3[] nearCornersWorld = new Vector3[4];
        Vector3[] farCornersWorld = new Vector3[4];
        cam.CalculateFrustumCorners(new Rect(0,0,1,1),cam.nearClipPlane,Camera.MonoOrStereoscopicEye.Mono, nearCornersWorld);
        cam.CalculateFrustumCorners(new Rect(0,0,1,1),cam.farClipPlane,Camera.MonoOrStereoscopicEye.Mono, farCornersWorld);
        for (int i = 0; i < 4; i++)
        {
            nearCornersWorld[i] = cam.transform.localToWorldMatrix * nearCornersWorld[i];
            farCornersWorld[i] = cam.transform.localToWorldMatrix * farCornersWorld[i];
            //Debug.DrawLine(transformed1,transformed2,Color.black);
            Vis.DrawLine(nearCornersWorld[i], farCornersWorld[i], thickness, c, Style.Unlit);
        }

        Vis.DrawConnectDots(nearCornersWorld, thickness, c, Style.Unlit);
        Vis.DrawConnectDots(farCornersWorld, thickness, c, Style.Unlit);
    }
    
    public static void QuickDrawLine(Vector3[] points, int p1, int p2, Color c, float thickness = 0.3f)
    {
        //Debug.DrawLine(points[p1],points[p2],Color.white);
        Vis.DrawLine(points[p1],points[p2],thickness, c, Style.Unlit);
    }

    private static Matrix4x4 CreateConvertionMatrixMinus11To01()
    {
        Matrix4x4 c = new Matrix4x4
        (new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
            new Vector4(0.5f, 0.5f, 0.5f, 1.0f)
        );
        return c;
    }

    public static Vector3 WorldToProjectedLocal(Vector3 pWorld, Camera c, bool normalizedTo01, out bool isInside)
    {
        //Point from World to Box
        Matrix4x4 camP = c.projectionMatrix;
        Vector4 pWorld4 = pWorld.ToVector4();
        Matrix4x4 scaleMatrix = Matrix4x4.identity;
        scaleMatrix.m22 = -1.0f;
        Matrix4x4 v = scaleMatrix * c.transform.worldToLocalMatrix;
        Matrix4x4 vp = camP * v;
        //get point projected in localSpace
        Vector4 pointProjected = vp * pWorld4;
        //correct projection by depthfactor -> now in box
        pointProjected /= pointProjected.w;
        Color cResult;
        //!!!! Only z-Tested !!!!
        isInside = (pointProjected.z <= 1 && pointProjected.z >= -1 && pointProjected.x <= 1 && pointProjected.x >= -1 &&
                     pointProjected.y <= 1 && pointProjected.y >= -1);
        if (normalizedTo01)
        {
            Matrix4x4 converter = CreateConvertionMatrixMinus11To01();
            pointProjected = converter *pointProjected; // same as MultiplyPoint3x4, ignore scaling
        }

        return pointProjected;
    }

    public static Vector3 ToWorld(Vector3 p, Transform t)
    {
        Vector3 pointProjectedInWorld = t.localToWorldMatrix * p.ToVector4();
        return pointProjectedInWorld;
    }

    private void DrawSamples()
    {
        Vector3 dir = camLookPoint.transform.position - camSymbol.transform.position;
        dir = dir.normalized;
        foreach (float f in samples)
        {
            pointInScene = camSymbol.transform.position+dir*f;
            Vis.DrawSphere(pointInScene,sphereSize,c2,Style.Unlit);
        }
    }
}
