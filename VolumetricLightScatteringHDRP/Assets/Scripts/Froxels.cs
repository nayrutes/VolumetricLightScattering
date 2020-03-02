using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

//[ExecuteInEditMode]
public class Froxels : MonoBehaviour
{
//    [Header("widthX, heightY, depthZ")]
    private Vector2 sizeNear, sizeFar; //defined by view frustrum

    [Header("inX, inY, inZ")]
    [SerializeField] private Vector3Int amount;

    [SerializeField] private AnimationCurve depthDistribution;
    
    private Camera _camera;
    // Start is called before the first frame update
    void Start()
    {
        _camera = GetComponent<Camera>();
    }

    private Frustum[] _froxels;
    
    struct Froxel
    {
//        public Vector3 corner;
//        public Rect near;
//        public Rect far;
//        public float depth;
        public Frustum frustum;
    }
    
    void Update()
    {
        //https://docs.unity3d.com/ScriptReference/Camera.CalculateFrustumCorners.html
        Vector3[] frustumCornersFar = new Vector3[4];
        Vector3[] frustumCornersNear = new Vector3[4];
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCornersFar);
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCornersNear);

        Vector3[] worldNear = new Vector3[4];
        Vector3[] worldFar = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            var worldSpaceCornerFar = _camera.transform.TransformVector(frustumCornersFar[i]);
            //var worldSpaceCornerNear = _camera.transform.TransformVector(frustumCornersNear[i]);
            //Debug.DrawRay(_camera.transform.position+frustumCornersNear[i], worldSpaceCornerFar, Color.blue);
            worldNear[i] = _camera.transform.position + frustumCornersNear[i];
            worldFar[i] = _camera.transform.position + frustumCornersFar[i];
        }

        sizeNear.x = Vector3.Distance(worldNear[1], worldNear[2]);
        sizeNear.y = Vector3.Distance(worldNear[0], worldNear[1]);

        sizeFar.x = Vector3.Distance(worldFar[1], worldFar[2]);
        sizeFar.y = Vector3.Distance(worldFar[0], worldFar[1]);
        
        DrawOutlineFrustrum(worldNear, worldFar);
        //DrawFroxels(worldNear , worldFar);
        GenerateFroxels(worldNear , worldFar);
        
        Vector3[] points = new Vector3[8];

        Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(_camera.projectionMatrix);
        
        points[0] = invViewProjMatrix.MultiplyPoint(new Vector3(-1, -0.3f, 0.3f));
        points[1] = invViewProjMatrix.MultiplyPoint(new Vector3(1, -1, 1));
        points[2] = invViewProjMatrix.MultiplyPoint(new Vector3(-1,  1, 1));
        points[3] = invViewProjMatrix.MultiplyPoint(new Vector3(1,  1, 1));
        points[4] = invViewProjMatrix.MultiplyPoint(new Vector3(-1, -1, -1));
        points[5] = invViewProjMatrix.MultiplyPoint(new Vector3(1, -1, -1));
        points[6] = invViewProjMatrix.MultiplyPoint(new Vector3(-1,  1, -1));
        points[7] = invViewProjMatrix.MultiplyPoint(new Vector3(1,  1, -1));

        for(int i=0; i<points.Length;i++)
        {

            points[i].z *= -1;
            points[i] = _camera.transform.localToWorldMatrix.MultiplyPoint(points[i]) ;
            DrawSphere(points[i],0.2f,Color.green);
        }
        
//        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, Camera.MonoOrStereoscopicEye.Left, frustumCornersFar);
//
//        for (int i = 0; i < 4; i++)
//        {
//            var worldSpaceCorner = _camera.transform.TransformVector(frustumCornersFar[i]);
//            Debug.DrawRay(_camera.transform.position, worldSpaceCorner, Color.green);
//        }
//
//        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, Camera.MonoOrStereoscopicEye.Right, frustumCornersFar);
//
//        for (int i = 0; i < 4; i++)
//        {
//            var worldSpaceCorner = _camera.transform.TransformVector(frustumCornersFar[i]);
//            Debug.DrawRay(_camera.transform.position, worldSpaceCorner, Color.red);
//        }
    }
    
    //Generate Froxels in Unit Cube, Transform them into the View Frustrum
    private void GenerateFroxels(Vector3[] nearCornersWorld, Vector3[] farCornersWorld)
    {
        
        Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(_camera.projectionMatrix);
        _froxels = new Frustum[amount.x*amount.y*amount.z];
        
        for (int k = 0; k < amount.z; k++)
        {
            //Depth generation from Back to Front
//            float inZFraction = ((1.0f / amount.z)) * k;
//            float inZConv = ConvertToNonLinear(inZFraction, _camera.nearClipPlane, _camera.farClipPlane);
//            
//            float inZFractionNear = ((1.0f / amount.z)) * (k+1);
//            float inZConvNear = ConvertToNonLinear(inZFractionNear, _camera.nearClipPlane, _camera.farClipPlane);
            float inZFar = ConvertRange01ToMinus11(((1.0f / amount.z) * k));
            float inZNear = ConvertRange01ToMinus11(((1.0f / amount.z) * (k+1)));
            
            for (int j = 0; j < amount.y; j++)
            {
//                Vector3 inY = nearCornersWorld[1] - nearCornersWorld[0];
//                Vector3 inYFraction = inY / amount.y;
                float inY = 1;
                float inYFractionBottom = ConvertRange01ToMinus11(inY * j / amount.y);
                float inYFractionTop = ConvertRange01ToMinus11(inY * (j+1) / amount.y);
                
                for (int i = 0; i < amount.x; i++)
                {
//                    Froxel f = new Froxel();
//                    f.depth = inZConv;
//                    f.near = new Rect();
                    //Vector3 inX = nearCornersWorld[2] - nearCornersWorld[1];
                    float inX = 1;
                    //float inXFractionLeft = inX * i / amount.x;
                    float inXFractionLeft = ConvertRange01ToMinus11(inX * i / amount.x);
                    float inXFractionRight = ConvertRange01ToMinus11(inX * (i + 1) / amount.x);
                    
                    Frustum frustum = new Frustum();

//                    Vector3 farLeftBottom = nearCornersWorld[0] + inYFraction * j + inXFraction * i + _camera.transform.forward * inZConv;/// (_camera.farClipPlane -_camera.nearClipPlane);
//                    //DrawSphere(farLeftBottom,0.5f,Color.magenta);
//                    
//                    Vector3 nearLeftBottom = nearCornersWorld[0] + inYFraction * j + inXFraction * i + _camera.transform.forward * inZConvNear;/// (_camera.farClipPlane -_camera.nearClipPlane);
//                    
//                    Vector3 farRightBottom = nearCornersWorld[0] + inYFraction * j + inXFraction * (i+1) + _camera.transform.forward * inZConv;
//                    Vector3 nearRightBottom = nearCornersWorld[0] + inYFraction * j + inXFraction * (i+1) + _camera.transform.forward * inZConvNear;
//                    
//                    Vector3 farLeftTop = nearCornersWorld[0] + inYFraction * (j+1) + inXFraction * i + _camera.transform.forward * inZConv;
//                    Vector3 nearLeftTop = nearCornersWorld[0] + inYFraction * (j+1) + inXFraction * i + _camera.transform.forward * inZConvNear;
//                    
//                    Vector3 farRightTop = nearCornersWorld[0] + inYFraction * (j+1) + inXFraction * (i+1) + _camera.transform.forward * inZConv;
//                    Vector3 nearRightTop = nearCornersWorld[0] + inYFraction * (j+1) + inXFraction * (i+1) + _camera.transform.forward * inZConvNear;
//                    
//                    
//                    //DrawSphere(nearRightBottom,0.5f, Color.red);
//                    //farLeftBottom, farRightBottom, farLeftTop, farRightTop, nearLeftBottom, nearRightBottom, nearLeftTop, nearRightTop
//                    frustum.corners = new[] {farLeftBottom,farRightBottom,farLeftTop,farRightTop,nearLeftBottom,nearRightBottom,nearLeftTop,nearRightTop};
                    
                    Vector3[] corners = new Vector3[8];
                    var transform1 = _camera.transform;
                    var forward = transform1.forward;
                    var right = transform1.right;
                    var up = transform1.up;
                    corners[0] = invViewProjMatrix.MultiplyPoint(forward * inZFar + right*inXFractionLeft + up* inYFractionBottom);
                    corners[1] = invViewProjMatrix.MultiplyPoint(forward * inZFar + right*inXFractionRight + up* inYFractionBottom);
                    corners[2] = invViewProjMatrix.MultiplyPoint(forward * inZFar + right*inXFractionLeft + up* inYFractionTop);
                    corners[3] = invViewProjMatrix.MultiplyPoint(forward * inZFar + right*inXFractionRight + up* inYFractionTop);
                    corners[4] = invViewProjMatrix.MultiplyPoint(forward * inZNear + right*inXFractionLeft + up* inYFractionBottom);
                    corners[5] = invViewProjMatrix.MultiplyPoint(forward * inZNear + right*inXFractionRight + up* inYFractionBottom);
                    corners[6] = invViewProjMatrix.MultiplyPoint(forward * inZNear + right*inXFractionLeft + up* inYFractionTop);
                    corners[7] = invViewProjMatrix.MultiplyPoint(forward * inZNear + right*inXFractionRight + up* inYFractionTop);
                    
                    for(int index=0; index<corners.Length;index++)
                    {

                        corners[index].z *= -1;
                        corners[index] = _camera.transform.localToWorldMatrix.MultiplyPoint(corners[index]) ;
                    }

                    frustum.corners = corners;
                    DrawFrustum(frustum);
                    
                    //frustum planes are inward-facing, generateClockwise
//                    Plane left = new Plane();
//                    left.Set3Points(nearLeftTop,farLeftTop,farLeftBottom);
//                    Plane right = new Plane();
//                    right.Set3Points(farRightBottom,farRightTop,nearRightTop);
//                    Plane top = new Plane();
//                        top.Set3Points(nearLeftTop,nearRightTop,farRightTop);
//                    Plane bottom = new Plane();
//                    bottom.Set3Points(nearLeftBottom, farLeftBottom, farRightBottom);
//                    Plane near = new Plane();
//                    near.Set3Points(nearRightTop,nearLeftTop,nearLeftBottom);
//                    Plane far = new Plane();
//                    far.Set3Points(farLeftTop,farRightTop,farRightBottom);
                    // Left, right, top, bottom, near, far
                    //frustum.planes = new[] {left, right, top, bottom, near, far};

                    _froxels[k * (amount.x * amount.y) + j * amount.x + i] = frustum;
                }
            }
        }
    }

    void DrawFrustum(Frustum f)
    {
        //farLeftBottom, farRightBottom, farLeftTop, farRightTop, nearLeftBottom, nearRightBottom, nearLeftTop, nearRightTop
        Debug.DrawLine(f.corners[4], f.corners[0],Color.cyan);
        Debug.DrawLine(f.corners[5], f.corners[1],Color.cyan);
        Debug.DrawLine(f.corners[6], f.corners[2],Color.cyan);
        Debug.DrawLine(f.corners[7], f.corners[3],Color.cyan);
        
        Debug.DrawLine(f.corners[4], f.corners[5],Color.cyan);
        Debug.DrawLine(f.corners[6], f.corners[7],Color.cyan);
        Debug.DrawLine(f.corners[0], f.corners[1],Color.cyan);
        Debug.DrawLine(f.corners[2], f.corners[3],Color.cyan);
        
        Debug.DrawLine(f.corners[4], f.corners[6],Color.cyan);
        Debug.DrawLine(f.corners[5], f.corners[7],Color.cyan);
        Debug.DrawLine(f.corners[0], f.corners[2],Color.cyan);
        Debug.DrawLine(f.corners[1], f.corners[3],Color.cyan);
    }
    
    void DrawFroxels(Vector3[] nearCornersWorld, Vector3[] farCornersWorld)
    {
        Vector3 inY = nearCornersWorld[1] - nearCornersWorld[0];
        Vector3 inYFraction = inY / amount.y;
        Vector3 inX = nearCornersWorld[2] - nearCornersWorld[1];
        Vector3 inXFraction = inX / amount.x;
        
        
        Vector3 inYFar = farCornersWorld[1] - farCornersWorld[0];
        Vector3 inYFractionFar = inYFar / amount.y;
        Vector3 inXFar = farCornersWorld[2] - farCornersWorld[1];
        Vector3 inXFractionFar = inXFar / amount.x;
        
        //DrawSphere(nearCornersWorld[2],0.2f);
        
        
        for (int i = 0; i < amount.x+1; i++) //+1 for last edge
        {
            for (int j = 0; j < amount.y+1; j++) //+1 for last edge
            {
                //Draw lines from front to back
                Debug.DrawLine(nearCornersWorld[0]+ inYFraction*j + inXFraction*i, farCornersWorld[0]+ inYFractionFar*j+inXFractionFar*i,Color.green);
            }
        }


        for (int k = 0; k < amount.z + 1; k++)
        {
            //Vector3 depthDir = _camera.transform.forward*(_camera.farClipPlane-_camera.nearClipPlane);
            
            float inZFraction = ((1.0f / amount.z)) * k;
            //float inZConv = ConvertToNonLinear(inZFraction, _camera.nearClipPlane, _camera.farClipPlane);
            

            for (int i = 0; i < amount.x + 1; i++)
            {
                
                
                Vector3 tmpBottomNear = nearCornersWorld[0] + inXFraction * i;
                Vector3 tmpTopNear = nearCornersWorld[1] + inXFraction * i;

                Vector3 tmpBottomFar = farCornersWorld[0] + inXFractionFar * i;
                Vector3 tmpTopFar = farCornersWorld[1] + inXFractionFar * i;

                Vector3 depthDirBottom = tmpBottomFar - tmpBottomNear;
                //Vector3 inZBottom = depthDirBottom.normalized * inZConv;

                Vector3 depthDirTop = tmpTopFar - tmpTopNear;
                //Vector3 inZTop = depthDirTop.normalized * inZConv;
                
                //Vector3 tmpStart = nearCornersWorld[0] + inXFraction * i + inZBottom;
                //Vector3 tmpEnd = nearCornersWorld[1] + inXFraction * i + inZTop;
                //Debug.DrawLine(tmpStart,tmpEnd,Color.yellow);
            }
        }
        
        
        
    }

    private float ConvertRange01ToMinus11(float value)
    {
        return value * 2 -1;
    }
    
    private float ConvertToNonLinear(float value)
    {
        //if (value == 0) value = 0.00001f;
        //return near*(1.0f/value) +far;
        //return (1.0f / value);
        //return (((1.0f / value* (far-near)) - near) / amount.z);
        //return (1.0f / value) * amount.z - near;
        //return Mathf.Log(value) * far;
        //return ((1.0f / (value + 1.0f)))*(far-near);
        //return depthDistribution.Evaluate(value)*(far-near);
        return depthDistribution.Evaluate(value);
    }
    
    void DrawOutlineFrustrum(Vector3[] nearCornersWorld, Vector3[] farCornersWorld)
    {
        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(nearCornersWorld[i],farCornersWorld[i],Color.cyan);
        }
    }
    
    
    public static void DrawSphere (Vector3 centre, float radius, Color color) {
        Debug.DrawRay(new Vector3(centre.x-radius,centre.y,centre.z), new  Vector3(radius*2 ,0,0), color);
        Debug.DrawRay(new Vector3(centre.x,centre.y-radius,centre.z), new  Vector3(0 ,radius*2,0), color);
        Debug.DrawRay(new Vector3(centre.x,centre.y,centre.z-radius), new  Vector3(0 ,0,radius*2), color);
    }
}
