using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

//[ExecuteInEditMode]
public class Froxels : MonoBehaviour
{
//    [Header("widthX, heightY, depthZ")]
    //private Vector2 sizeNear, sizeFar; //defined by view frustrum

    [SerializeField] private ComputeShader scatteringCompute;
    [SerializeField] private RenderTexture scatteringInput;
    [FormerlySerializedAs("scatteringInputWithShadows")] [SerializeField] private RenderTexture densityBufferTexture;
    [SerializeField] private RenderTexture lightBufferTexture;
    [SerializeField] private RenderTexture scatteringOutput;
    [SerializeField] private RenderTexture camDepthTexture;
    [SerializeField] private RenderTexture scatteringResult2D;
    [SerializeField] private Material from3DTo2D;
    //[SerializeField] private Material renderMaterial;
    //[SerializeField] private Material fullScreenPassHdrpMaterial;
    [SerializeField] private ComputeShader orientCompute;
    [SerializeField] private ComputeShader densityCompute;
    [SerializeField] private Vector4 insetValue;
    [SerializeField] private ShadowManager shadowManager;
    
    [Header("inX, inY, inZ")]
    [SerializeField] private Vector3Int amount;

    public bool enableDebugDraw = true;
    public bool drawCorners = true;
    public bool drawEdges = true;
    public bool toggleSingleAll = false;
    public Vector3Int singleFroxel;
    public bool orientFroxels = true;
    public bool calculateDensity = true;
    public bool enableScatteringCompute = true;
    public bool enableGenerateFroxelsEveryFrame = true;
    public bool enableCalculateShadows = true;
    public bool enableTransformedChilds = true;
    
    [SerializeField] private AnimationCurve depthDistribution;
    
    private Camera _camera;
    private Froxel[] _froxels;
    private float[] depths;
    private FroxelFlat[] ff;
    ComputeBuffer cb;
    private List<GameObject> pointsGos = new List<GameObject>();
    
    private DebugSlice _debugSlice;
    private Vector3 froxelLastPositionOrigin;
    private Matrix4x4 lastFrameWorldToLocal;
    
    private Vector4[] points4;
    
    public struct Froxel
    {
        //public Frustum frustum;
        public Vector3 center;
        public Vector3[] corners; // Positions of the 8 corners
        public Transform goT;
    }
    
    public struct FroxelFlat
    {
        //public Frustum frustum;
        public Vector3 center;
//        public Vector3 corner0; // Positions of the 8 corners
//        public Vector3 corner1;
//        public Vector3 corner2;
//        public Vector3 corner3;
//        public Vector3 corner4;
//        public Vector3 corner5;
//        public Vector3 corner6;
//        public Vector3 corner7;
    }
    
    void Start()
    {
        _camera = GetComponent<Camera>();
        _debugSlice = FindObjectOfType<DebugSlice>();
       // _debugSlice.texture3DToSlice = scatteringOutput;
        //GenerateFroxels();
        GenerateFroxelsInWorldSpace();
    }
    
    public void SetUp(int froxelCount)
    {
        points4 = new Vector4[froxelCount];
        for (var i = 0; i < points4.Length; i++)
        {
            points4[i] = new Vector4(0,0,0,1);
        }
    }
    
    void Update()
    {
        DrawOutlineFrustrum();
        if (enableGenerateFroxelsEveryFrame)
        {
            //destroy old points
            foreach (GameObject g in pointsGos)
            {
                Destroy(g);
            }
            //GenerateFroxels();
            GenerateFroxelsInWorldSpace();
        }
        else
        {
            if (orientFroxels && !enableTransformedChilds)
            {
                //not necessary with transform in child
                OrientFroxelsComputeShader();
            }
        }
        
        if (enableDebugDraw)
        {
            if (toggleSingleAll)
            {
                foreach (Froxel froxel in _froxels)
                {
                    DrawFrustum(froxel);
                }
            }
            else
            {
                DrawFrustum(_froxels[singleFroxel.z * (amount.x * amount.y) + singleFroxel.y * amount.x + singleFroxel.x]);
            }
        }

        TransferPoint4(_froxels);
        
        if (calculateDensity)
        {
            CalculateDensity();
        }
        
        if (enableCalculateShadows)
        {
            CalculateShadows();
        }
        else
        {
            densityBufferTexture = scatteringInput;
        }

        if (enableScatteringCompute)
        {
            camDepthTexture.Release();
            camDepthTexture.width = _camera.scaledPixelWidth;
            camDepthTexture.height = _camera.scaledPixelHeight;
            camDepthTexture.Create();
            _camera.targetTexture = camDepthTexture;
            _camera.Render();
            _camera.targetTexture = null;
            RunScatteringCompute();
        }
        
        From3DTo2D();
        
        //fullScreenPassHdrpMaterial.SetVector("_Amount", new Vector4(amount.x,amount.y,amount.z,0));
        //fullScreenPassHdrpMaterial.SetTexture("VolumetricFogSampler",scatteringOutput);
        


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

    private void GenerateFroxelsInWorldSpace()
    {
        depths = new float[amount.z+1];
        for (int z = 0; z <= amount.z; z++)
        {
            float zl = Mathf.InverseLerp(0, amount.z, z);
            zl = CreateNonLinear(zl);
            depths[z] = zl;
        }
        
        
        lastFrameWorldToLocal = _camera.transform.worldToLocalMatrix;
        _froxels = new Froxel[amount.x*amount.y*amount.z];
        ff = new FroxelFlat[_froxels.Length];
        cb = new ComputeBuffer(ff.Length,sizeof(float)*3/*+sizeof(float)*3*8*/);
        SetUp(_froxels.Length);
        
        Vector3[] fC = new Vector3[4];
        Vector3[] nC = new Vector3[4];
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, fC);
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nC);
        //Debug.Log(String.Join("",fC.ToList().ConvertAll(i => i.ToString()).ToArray()));
        Vector3[] points = new Vector3[(amount.x+1)*(amount.y+1)*(amount.z+1)];
        for (int x = 0; x <= amount.x; x++)
        {
            float xl = Mathf.InverseLerp(0, amount.x, x);
            
            for (int y = 0; y <= amount.y; y++)
            {
                float yl = Mathf.InverseLerp(0, amount.y, y);
                for (int z = 0; z <= amount.z; z++)
                {
                    float zl = depths[z];
                    Vector3 point = InterP(new Vector3(xl, yl, zl), nC, fC);

                    point = _camera.transform.localToWorldMatrix.MultiplyPoint3x4(point);
                    points[z * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + x] = point;
                }
            }
        }
        //Debug.Log(String.Join("",points.ToList().ConvertAll(i => i.ToString()).ToArray()));
//        foreach (Vector3 point in points)
//        {
//            DrawSphere(point,0.5f,Color.magenta);
//        }
        
        for (int x = 0; x < amount.x; x++)
        {
            for (int y = 0; y < amount.y; y++)
            {
                for (int z = 0; z < amount.z; z++)
                {
                    //lbf, rbf, ltf, rtf, lbn, rbn, ltn, rtn,
                    Vector3[] corners = new Vector3[]
                    {
                        points[(z+1) * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + x],
                        points[(z+1) * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + (x+1)],
                        points[(z+1) * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + x],
                        points[(z+1) * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + (x+1)],
                        points[z * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + x],
                        points[z * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + (x+1)],
                        points[z * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + x],
                        points[z * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + (x+1)]
                    };
                    //Frustum f = GenerateFrustum(corners);
                    Froxel froxel = new Froxel();
                    froxel.corners = corners;
                    froxel.center = CalculateCenter(froxel);
                    GameObject go =SpawnPointChild(froxel.center);
                    froxel.goT = go.transform;
                    pointsGos.Add(go);
                    _froxels[z * ((amount.x) * (amount.y)) + y * (amount.x) + x]= froxel;
                }
            }
        }
    }
    
    private GameObject SpawnPointChild(Vector3 point)
    {
        GameObject go = new GameObject();
        GameObject inst = Instantiate(go, point, new Quaternion(0,0,0,0), this.transform);
        inst.name = $"point - {point.ToString()}";
        Destroy(go);
        return inst;
    }
    
    private Vector3 InterP(Vector3 iL, Vector3[] nPC, Vector3[] fPC)
    {
        Vector3 onXNear1 = Vector3.Lerp(nPC[0], nPC[3], iL.x);
        Vector3 onXNear2 = Vector3.Lerp(nPC[1], nPC[2], iL.x);
        Vector3 onYNear = Vector3.Lerp(onXNear1, onXNear2, iL.y);
        
        Vector3 onXFar1 = Vector3.Lerp(fPC[0], fPC[3], iL.x);
        Vector3 onXFar2 = Vector3.Lerp(fPC[1], fPC[2], iL.x);
        Vector3 onYFar = Vector3.Lerp(onXFar1, onXFar2, iL.y);

        return Vector3.Lerp(onYNear, onYFar, iL.z);
    }

    private Frustum GenerateFrustum(Vector3[] corners)
    {
        Frustum f = new Frustum();
        f.corners = corners;
        return f;
    }
    
    //Generate Froxels in Unit Cube, Transform them into the View Frustrum
//    private void GenerateFroxels()
//    {
//        froxelLastPositionOrigin = _camera.transform.position;
//        lastFrameWorldToLocal = _camera.transform.worldToLocalMatrix;
//        
//        _froxels = new Frustum[amount.x*amount.y*amount.z];
//        
//        //Froxelize Unitcube
//        for (int k = 0; k < amount.z; k++)
//        {
//            float inZFar = ((1.0f / amount.z) * k);
//            float inZNear = ((1.0f / amount.z) * (k+1));
//            
//            //reverse projection non-linear depth before applying projMatrix
//            float f = _camera.farClipPlane;
//            float n = _camera.nearClipPlane;
//            float ratio = f / n;
//
//
//                inZNear = (1-1 / (inZNear));
//
//                inZFar = (1-1 / (inZFar));
//
//            
//            inZFar = ConvertRange01ToMinus11(inZFar);
//            inZNear = ConvertRange01ToMinus11(inZNear);
//            
//
//            for (int j = 0; j < amount.y; j++)
//            {
//                float inY = 1;
//                float inYFractionBottom = ConvertRange01ToMinus11(inY * j / amount.y);
//                float inYFractionTop = ConvertRange01ToMinus11(inY * (j+1) / amount.y);
//                
//                for (int i = 0; i < amount.x; i++)
//                {
//                    float inX = 1;
//                    float inXFractionLeft = ConvertRange01ToMinus11(inX * i / amount.x);
//                    float inXFractionRight = ConvertRange01ToMinus11(inX * (i + 1) / amount.x);
//                    
//                    Frustum frustum = new Frustum();
//                    
//                    Vector3[] corners = new Vector3[8];
//                    var forward = Vector3.forward;
//                    var right = Vector3.right;
//                    var up = Vector3.up;
//                    corners[0] = (forward * inZFar + right*inXFractionLeft + up* inYFractionBottom);
//                    corners[1] = (forward * inZFar + right*inXFractionRight + up* inYFractionBottom);
//                    corners[2] = (forward * inZFar + right*inXFractionLeft + up* inYFractionTop);
//                    corners[3] = (forward * inZFar + right*inXFractionRight + up* inYFractionTop);
//                    corners[4] = (forward * inZNear + right*inXFractionLeft + up* inYFractionBottom);
//                    corners[5] = (forward * inZNear + right*inXFractionRight + up* inYFractionBottom);
//                    corners[6] = (forward * inZNear + right*inXFractionLeft + up* inYFractionTop);
//                    corners[7] = (forward * inZNear + right*inXFractionRight + up* inYFractionTop);

    //frustum.corners = corners;
                    
//                    _froxels[k * (amount.x * amount.y) + j * amount.x + i] = frustum;
                    
                    //OrientFroxels();
//                }
//            }
//        }
        //OrientFroxels();
//        ProjectUnitCubeToFrustrum();
//    }

//    void ProjectUnitCubeToFrustrum()
       //    {
       //        //TODO check if better use Non-Jittered Projection matrix https://docs.unity3d.com/ScriptReference/Camera-nonJitteredProjectionMatrix.html
       //        Matrix4x4 invViewProjMatrix = _camera.projectionMatrix.inverse;
       //        var projectionMatrix = _camera.projectionMatrix;
       //        var m = _camera.worldToCameraMatrix;
       //        Matrix4x4 customProjMatrix = new Matrix4x4();//s = projectionMatrix;
       //        customProjMatrix.m00 = projectionMatrix.m00;
       //        customProjMatrix.m11 = projectionMatrix.m11;
       //        customProjMatrix.m32 = projectionMatrix.m32;
       //        //customProjMatrix.m33 = projectionMatrix.m33;
       //        customProjMatrix.m23 = projectionMatrix.m23;
       //        //customProjMatrix.
       //        foreach (Frustum froxel in _froxels)
       //        {
       //            float f = _camera.farClipPlane;
       //            float n = _camera.nearClipPlane;
       //            float nTmp = n;
       //            
       //            for(int index=0; index<froxel.corners.Length;index++)
       //            {
       //                Vector3 p = froxel.corners[index];
       //                froxel.corners[index] = projectionMatrix.inverse.MultiplyPoint(p);
       //                froxel.corners[index].z *= -1;
       //                froxel.corners[index] = _camera.transform.localToWorldMatrix.MultiplyPoint3x4(froxel.corners[index]);
       //            }
       //        }
       //    }
    
    void OrientFroxels()
    {
        //Matrix4x4 inv = Matrix4x4.Inverse(lastFrameWorldToLocal);
        Matrix4x4 comb = _camera.transform.localToWorldMatrix * lastFrameWorldToLocal;
        for (var i = 0; i < _froxels.Length; i++)
        {
            for (int index = 0; index < _froxels[i].corners.Length; index++)
            {
                //froxel.corners[index] = lastFrameWorldToLocal.MultiplyPoint(froxel.corners[index]);
                //froxel.corners[index] = _camera.transform.localToWorldMatrix.MultiplyPoint(froxel.corners[index]) ;
                _froxels[i].corners[index] = comb.MultiplyPoint3x4(_froxels[i].corners[index]);
            }

            _froxels[i].center = comb.MultiplyPoint3x4(_froxels[i].center);
        }

        lastFrameWorldToLocal = _camera.transform.worldToLocalMatrix;
    }

    void OrientFroxelsComputeShader()
    {
        //var stopwatch = new Stopwatch();
        //stopwatch.Start();

        
        for (var i = 0; i < _froxels.Length; i++)
        {
            //GetFlat(ref _froxels[i], ref ff[i]);
            ff[i].center = _froxels[i].center;
//            ff[i].corner0 = _froxels[i].corners[0];
//            ff[i].corner1 = _froxels[i].corners[1];
//            ff[i].corner2 = _froxels[i].corners[2];
//            ff[i].corner3 = _froxels[i].corners[3];
//            ff[i].corner4 = _froxels[i].corners[4];
//            ff[i].corner5 = _froxels[i].corners[5];
//            ff[i].corner6 = _froxels[i].corners[6];
//            ff[i].corner7 = _froxels[i].corners[7];
        }
        //Debug.Log("in1: "+stopwatch.ElapsedMilliseconds);
        cb.SetData(ff);
        //Debug.Log("in2: "+stopwatch.ElapsedMilliseconds);
        
        orientCompute.SetBuffer(0,"froxels",cb);
        Matrix4x4 comb = _camera.transform.localToWorldMatrix * lastFrameWorldToLocal;
        orientCompute.SetMatrix("comb",comb);
        
        orientCompute.Dispatch(0,ff.Length/1024,1,1);
        
        cb.GetData(ff);
        //cb.Dispose();
        //Debug.Log("out1: "+stopwatch.ElapsedMilliseconds);
        for (var i = 0; i < _froxels.Length; i++)
        {
            //GetFull(ref ff[i], ref _froxels[i]);
//            _froxels[i].corners[0] = ff[i].corner0;
//            _froxels[i].corners[1] = ff[i].corner1;
//            _froxels[i].corners[2] = ff[i].corner2;
//            _froxels[i].corners[3] = ff[i].corner3;
//            _froxels[i].corners[4] = ff[i].corner4;
//            _froxels[i].corners[5] = ff[i].corner5;
//            _froxels[i].corners[6] = ff[i].corner6;
//            _froxels[i].corners[7] = ff[i].corner7;

            _froxels[i].center = ff[i].center;
        }
        //stopwatch.Stop();
        //Debug.Log("out2: "+stopwatch.ElapsedMilliseconds);

        if (enableDebugDraw)
        {
            int debugFroxel = singleFroxel.z * (amount.x * amount.y) + singleFroxel.y * amount.x + singleFroxel.x;
            for (int index = 0; index < _froxels[debugFroxel].corners.Length; index++)
            {
                //froxel.corners[index] = lastFrameWorldToLocal.MultiplyPoint(froxel.corners[index]);
                //froxel.corners[index] = _camera.transform.localToWorldMatrix.MultiplyPoint(froxel.corners[index]) ;
                _froxels[debugFroxel].corners[index] = comb.MultiplyPoint3x4(_froxels[debugFroxel].corners[index]);
            }
        }
        
        lastFrameWorldToLocal = _camera.transform.worldToLocalMatrix;
    }
    
//    void AdjustFroxelsPosition()
//    {
//        Vector3 moved = _camera.transform.position - froxelLastPositionOrigin;
//        froxelLastPositionOrigin = _camera.transform.position;
//        foreach (Frustum froxel in _froxels)
//        {
//            for(int index=0; index<froxel.corners.Length;index++)
//            {
//                //froxel.corners[index] += moved;
//            }
//        }
//    }

    private void From3DTo2D()
    {
        //AdjustRenderTexture(scatteringResult2D, false, false);
        Graphics.Blit(null,scatteringResult2D,from3DTo2D);
    }

    void DrawFrustum(Froxel f)
    {
        //farLeftBottom, farRightBottom, farLeftTop, farRightTop, nearLeftBottom, nearRightBottom, nearLeftTop, nearRightTop
        if (drawEdges && !enableTransformedChilds)
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

        if (drawCorners && !enableTransformedChilds)
        {
//            foreach (Vector3 corner in f.corners)
//            {
//                DrawSphere(corner,0.1f,Color.magenta);
//            }
            for (int i = 0; i < f.corners.Length; i++ )
            {
                //Color c = Color.black;
                
            }
            //lbf, rbf, ltf, rtf, lbn, rbn, ltn, rtn,
            DrawPointCross(f.corners[0],0.2f,new Color(0,0,1));
            DrawPointCross(f.corners[1],0.2f,new Color(1,0,1));
            DrawPointCross(f.corners[2],0.2f,new Color(0,1,1));
            DrawPointCross(f.corners[3],0.2f,new Color(1,1,1));
            DrawPointCross(f.corners[4],0.2f,new Color(0,0,0));
            DrawPointCross(f.corners[5],0.2f,new Color(1,0,0));
            DrawPointCross(f.corners[6],0.2f,new Color(0,1,0));
            DrawPointCross(f.corners[7],0.2f,new Color(1,1,0));
        }

        if (enableTransformedChilds)
        {
            DrawPointCross(f.goT.position,0.2f,new Color(0.5f,0.5f,0.5f));
        }
    }

    private float ConvertRange01ToMinus11(float value)
    {
        return value * 2 -1;
    }
    
    private float CreateNonLinear(float value)
    {
        float result = depthDistribution.Evaluate(value);
        return result;
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
        Vector4 size = new Vector4(densityBufferTexture.width, densityBufferTexture.height,
            densityBufferTexture.volumeDepth, 0);
        
        AdjustRenderTexture(scatteringOutput);
        
        scatteringCompute.SetTexture(0, "DensityBuffer",densityBufferTexture);
        scatteringCompute.SetTexture(0, "LightBuffer",lightBufferTexture);
        scatteringCompute.SetTexture(0, "Result",scatteringOutput);
        scatteringCompute.SetTexture(0,"CamDepth",camDepthTexture);
        scatteringCompute.SetVector("insetValue",insetValue);
        scatteringCompute.SetVector("size", size);
        float f = _camera.farClipPlane;
        float n = _camera.nearClipPlane;
        Vector4 zBufferParam = new Vector4((f-n)/n, 1, (f-n)/n*f, 1 / f);
        scatteringCompute.SetVector("_ZBufferParams",zBufferParam);
        ComputeBuffer cb2 = new ComputeBuffer(depths.Length,sizeof(float));
        cb2.SetData(depths);
        scatteringCompute.SetBuffer(0,"depths",cb2);
        //int threadGroupsX = amount.z / 256;
        scatteringCompute.SetInt("VOLUME_DEPTH",amount.z);
        scatteringCompute.Dispatch(0, (int)(size.x/32.0f),(int)(size.y/32.0f),1);
        
        cb2.Dispose();
    }

    private void CalculateDensity()
    {
        AdjustRenderTexture(densityBufferTexture);
        densityCompute.SetTexture(0, "densityBufferTexture", densityBufferTexture);
        densityCompute.SetVector("Input_TexelSize",new Vector4(densityBufferTexture.width,densityBufferTexture.height,densityBufferTexture.volumeDepth,0));
        ComputeBuffer cb3 = new ComputeBuffer(points4.Length,sizeof(float)*4);
        cb3.SetData(points4);
        densityCompute.SetBuffer(0,"points",cb3);
        densityCompute.Dispatch(0,densityBufferTexture.height/8,densityBufferTexture.width/8,densityBufferTexture.volumeDepth/16);
        
        cb3.Dispose();
    }
    
    private void CalculateShadows()
    {
        //var watch = System.Diagnostics.Stopwatch.StartNew();

        //TODO prepare rendertextures
        AdjustRenderTexture(lightBufferTexture);
        //AdjustRenderTexture(scatteringInput);
        //watch.Stop();
        //var elapsedMs = watch.ElapsedMilliseconds;
        //Debug.Log(elapsedMs);
        
        
        shadowManager.CalculateShadows(lightBufferTexture, points4, enableTransformedChilds);
    }

    

    public static void DrawPointCross (Vector3 centre, float radius, Color color) {
        Debug.DrawRay(new Vector3(centre.x-radius,centre.y,centre.z), new  Vector3(radius*2 ,0,0), color);
        Debug.DrawRay(new Vector3(centre.x,centre.y-radius,centre.z), new  Vector3(0 ,radius*2,0), color);
        Debug.DrawRay(new Vector3(centre.x,centre.y,centre.z-radius), new  Vector3(0 ,0,radius*2), color);
    }

    public Vector3 CalculateCenter(Froxel f)
    {
        Vector3 result = new Vector3(0,0,0);
        foreach (Vector3 corner in f.corners)
        {
            result += corner;
        }

        result /= 8;
        return result;
    }

    private void AdjustRenderTexture(RenderTexture r, bool depth = true, bool randomwrite = true)
    {
        r.Release();
        r.width = amount.x;
        r.height = amount.y;
        if(depth)
            r.volumeDepth = amount.z;
        if(randomwrite)
            r.enableRandomWrite = true;
        r.Create();
    }

    private void TransferPoint4(Froxels.Froxel[] froxel)
    {
        for (var i = 0; i < froxel.Length; i++)
        {
            if (enableTransformedChilds)
            {
                froxel[i].goT.position.ToVector4(ref points4[i]);
            }
            else
            {
                //froxel[i].center.ToVector4(ref points4[i]);
                points4[i].x = froxel[i].center.x;
                points4[i].y = froxel[i].center.y;
                points4[i].z = froxel[i].center.z;
                points4[i].w = 1;
            }
        }
    }
    
//    public static void GetFlat(ref Froxel f, ref FroxelFlat ff)
//    {
//        //FroxelFlat ff = new FroxelFlat
//        //{
//        ff.center = f.center;
//        ff.corner0 = f.corners[0];
//        ff.corner1 = f.corners[1];
//        ff.corner2 = f.corners[2];
//        ff.corner3 = f.corners[3];
//        ff.corner4 = f.corners[4];
//        ff.corner5 = f.corners[5];
//        ff.corner6 = f.corners[6];
//        ff.corner7 = f.corners[7];
//        //};
//        //return ff;
//    }
//    
//    public static void GetFull(ref FroxelFlat ff, ref Froxel f)
//    {
//        //Vector3[] corners = new Vector3[8];
//
//        f.corners[0] = ff.corner0;
//        f.corners[1] = ff.corner1;
//        f.corners[2] = ff.corner2;
//        f.corners[3] = ff.corner3;
//        f.corners[4] = ff.corner4;
//        f.corners[5] = ff.corner5;
//        f.corners[6] = ff.corner6;
//        f.corners[7] = ff.corner7;
//
//        f.center = ff.center;
////        Froxel f = new Froxel
////        {
////            center = ff.center,
////            corners = corners
////        };
//        //return f;
//    }
}
