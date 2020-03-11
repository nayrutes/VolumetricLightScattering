using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ProjectionTest : MonoBehaviour
{
    private Camera cam;
    [SerializeField] bool normalizedBox;

    [Header("!!!! Only z-Tested !!!!")]
    [SerializeField] private Vector3 pointInScene;
    
    //public bool feedBackIsOutside = false;
    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        DrawDebugCube();
        TestProjection();
    }
    
    public void TestProjection()
    {
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
        
        Vector3 pointProjectedInWorld = cam.transform.localToWorldMatrix * pointProjected;
        
        Froxels.DrawPointCross(pointInScene,0.5f,Color.cyan);
        Froxels.DrawPointCross(pointProjectedInWorld,0.5f,cResult);
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
