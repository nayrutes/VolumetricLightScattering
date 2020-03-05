using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

//[ExecuteInEditMode]
public class Froxels : MonoBehaviour
{
//    [Header("widthX, heightY, depthZ")]
    //private Vector2 sizeNear, sizeFar; //defined by view frustrum

    [SerializeField] private ComputeShader scatteringCompute;
    [SerializeField] private RenderTexture scatteringInput;
    [SerializeField] private RenderTexture scatteringOutput;
    [SerializeField] private Material renderMaterial;
    [SerializeField] private Material fullScreenPassHdrpMaterial;
    [SerializeField] private Vector4 insetValue;
    
    [Header("inX, inY, inZ")]
    [SerializeField] private Vector3Int amount;

    public bool enableDebugDraw = true;
    public bool drawCorners = true;
    public bool drawEdges = true;
    public bool toggleSingleAll = false;
    public Vector3Int singleFroxel;
    public bool enableScatteringCompute = true;
    public bool enableGenerateFroxelsEveryFrame = true;
    
    [Header("unused")]
    [SerializeField] private AnimationCurve depthDistribution;
    
    private Camera _camera;
    private Frustum[] _froxels;
    struct Froxel
    {
        public Frustum frustum;
    }
    
    void Start()
    {
        _camera = GetComponent<Camera>();
        GenerateFroxels();
    }
    
    void Update()
    {
        DrawOutlineFrustrum();
        if(enableGenerateFroxelsEveryFrame)
            GenerateFroxels();
        if (enableDebugDraw)
        {
            if (toggleSingleAll)
            {
                foreach (Frustum frustum in _froxels)
                {
                    DrawFrustum(frustum);
                }
            }
            else
            {
                DrawFrustum(_froxels[singleFroxel.z * (amount.x * amount.y) + singleFroxel.y * amount.x + singleFroxel.x]);
            }
        }
        if(enableScatteringCompute)
            RunScatteringCompute();
        
        fullScreenPassHdrpMaterial.SetVector("_Amount", new Vector4(amount.x,amount.y,amount.z,0));
        fullScreenPassHdrpMaterial.SetTexture("VolumetricFogSampler",scatteringOutput);
        


        //Testing Projection
//        Vector3[] points = new Vector3[8];
//
//        Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(_camera.projectionMatrix);
//        
//        points[0] = invViewProjMatrix.MultiplyPoint(new Vector3(-1, -0.3f, 0.3f));
//        points[1] = invViewProjMatrix.MultiplyPoint(new Vector3(1, -1, 1));
//        points[2] = invViewProjMatrix.MultiplyPoint(new Vector3(-1,  1, 1));
//        points[3] = invViewProjMatrix.MultiplyPoint(new Vector3(1,  1, 1));
//        points[4] = invViewProjMatrix.MultiplyPoint(new Vector3(-1, -1, -1));
//        points[5] = invViewProjMatrix.MultiplyPoint(new Vector3(1, -1, -1));
//        points[6] = invViewProjMatrix.MultiplyPoint(new Vector3(-1,  1, -1));
//        points[7] = invViewProjMatrix.MultiplyPoint(new Vector3(1,  1, -1));
//
//        for(int i=0; i<points.Length;i++)
//        {
//            points[i].z *= -1;
//            points[i] = _camera.transform.localToWorldMatrix.MultiplyPoint(points[i]) ;
//            DrawSphere(points[i],0.2f,Color.green);
//        }
    }
    
    //Generate Froxels in Unit Cube, Transform them into the View Frustrum
    private void GenerateFroxels()
    {
        
        Matrix4x4 invViewProjMatrix = Matrix4x4.Inverse(_camera.projectionMatrix);
        _froxels = new Frustum[amount.x*amount.y*amount.z];
        
        for (int k = 0; k < amount.z; k++)
        {
            float inZFar = ConvertRange01ToMinus11(((1.0f / amount.z) * k));
            float inZNear = ConvertRange01ToMinus11(((1.0f / amount.z) * (k+1)));
            
            for (int j = 0; j < amount.y; j++)
            {
                float inY = 1;
                float inYFractionBottom = ConvertRange01ToMinus11(inY * j / amount.y);
                float inYFractionTop = ConvertRange01ToMinus11(inY * (j+1) / amount.y);
                
                for (int i = 0; i < amount.x; i++)
                {
                    float inX = 1;
                    float inXFractionLeft = ConvertRange01ToMinus11(inX * i / amount.x);
                    float inXFractionRight = ConvertRange01ToMinus11(inX * (i + 1) / amount.x);
                    
                    Frustum frustum = new Frustum();
                    
                    Vector3[] corners = new Vector3[8];
                    var forward = Vector3.forward;
                    var right = Vector3.right;
                    var up = Vector3.up;
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
                    //DrawFrustum(frustum);
                    
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
        if (drawEdges)
        {
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

        if (drawCorners)
        {
            foreach (Vector3 corner in f.corners)
            {
                DrawSphere(corner,0.1f,Color.magenta);
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
    
    void DrawOutlineFrustrum()
    {
//        for (int i = 0; i < 4; i++)
//        {
//            Debug.DrawLine(nearCornersWorld[i],farCornersWorld[i],Color.cyan);
//        }
    }


    void RunScatteringCompute()
    {
        
//        RenderTexture textureInput = new RenderTexture(amount.x,amount.y,0,RenderTextureFormat.ARGBFloat,0);
//        textureInput.dimension = TextureDimension.Tex3D;
//        textureInput.volumeDepth = amount.z;
//        //textureInput.filterMode = 
//        textureInput.enableRandomWrite = true;
//        RenderTexture textureOutput = new RenderTexture(amount.x,amount.y,0,RenderTextureFormat.ARGBFloat,0);
//        textureOutput.dimension = TextureDimension.Tex3D;
//        textureOutput.volumeDepth = amount.z;
//        textureOutput.enableRandomWrite = true;


        scatteringInput.Release();
        scatteringOutput.Release();
        scatteringInput.width = scatteringOutput.width = amount.x;
        scatteringInput.height = scatteringOutput.height = amount.y;
        scatteringInput.volumeDepth = scatteringOutput.volumeDepth = amount.z;
        scatteringInput.enableRandomWrite = true;
        scatteringOutput.enableRandomWrite = true;
        scatteringInput.Create();
        scatteringOutput.Create();
        
        scatteringCompute.SetTexture(0, "Input",scatteringInput);
        scatteringCompute.SetTexture(0, "Result",scatteringOutput);
        scatteringCompute.SetVector("insetValue",insetValue);
        
        int threadGroupsX = amount.z / 256;
        scatteringCompute.Dispatch(0, threadGroupsX,1,1);
        
        scatteringCompute.SetInt("VOLUME_DEPTH",amount.z);
    }

//    private void OnRenderImage(RenderTexture src, RenderTexture dest)
//    {
//        //renderMaterial.SetTexture();
//        Graphics.Blit(src,dest,renderMaterial);
//    }


    public static void DrawSphere (Vector3 centre, float radius, Color color) {
        Debug.DrawRay(new Vector3(centre.x-radius,centre.y,centre.z), new  Vector3(radius*2 ,0,0), color);
        Debug.DrawRay(new Vector3(centre.x,centre.y-radius,centre.z), new  Vector3(0 ,radius*2,0), color);
        Debug.DrawRay(new Vector3(centre.x,centre.y,centre.z-radius), new  Vector3(0 ,0,radius*2), color);
    }
}
