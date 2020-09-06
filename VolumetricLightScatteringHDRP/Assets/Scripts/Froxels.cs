using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;
using Visualisation;
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
    [SerializeField] public Vector3Int amount;

    public bool enableDebugDraw = true;
    public bool drawCorners = true;
    public bool drawEdges = true;
    public float frustrumThickness;
    public bool drawSingle = false;
    public bool drawAll = false;
    public bool toggleSingleAll = false;
    public Vector3Int singleFroxel;
    public bool orientFroxels = true;
    public bool calculateDensity = true;
    public bool enableScatteringCompute = true;
    public bool enableGenerateFroxelsEveryFrame = true;
    public bool enableCalculateShadows = true;
    public bool enableTransformedChilds = true;
    //public CustomPass effectPassVolume;
    public bool disableResult;
    //https://bitbucket.org/Unity-Technologies/cinematic-image-effects/src/96901525f6864a62aeadec288cc7749fef4c70a9/UnityProject/Assets/Standard%20Assets/Effects/CinematicEffects(BETA)/TonemappingColorGrading/TonemappingColorGrading.cs
    [SerializeField] private AnimationCurve depthDistribution;
    [SerializeField] private AnimationCurve depthDistribution2;
    [SerializeField] private bool useDepthdistribution2;
    [SerializeField] private RenderTexture CurveOutput;
    private Texture2D m_CurveTexture;
    private Texture2D curveTexture
    {
        get
        {
            if (m_CurveTexture == null)
            {
                m_CurveTexture = new Texture2D(256, 1, TextureFormat.ARGB32, false, true)
                {
                    name = "Curve texture",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    hideFlags = HideFlags.DontSave
                };
            }

            return m_CurveTexture;
        }
    }
    [HideInInspector]
    public Vector3[] pointsCamRelative;
    public Camera _camera;
    [HideInInspector]
    public Froxel[] _froxelsCamRelative;
    private Froxel[] _froxels;
    private float[] depths;
    private FroxelFlat[] ff;
    private ComputeBuffer cbOrientOutput;
    ComputeBuffer cbOrientInput;
    private List<GameObject> pointsGos = new List<GameObject>();
    
    private DebugSlice _debugSlice;
    private Vector3 froxelLastPositionOrigin;
    private Matrix4x4 lastFrameWorldToLocal;
    
    private Vector4[] points4;
    private CustomPassVolume[] cpvs;
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

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
        _debugSlice = FindObjectOfType<DebugSlice>();
        cpvs = GetComponents<CustomPassVolume>();
    }

    private void OnDisable()
    {
        CleanUpOrient();
    }

    void Start()
    {
        
       // _debugSlice.texture3DToSlice = scatteringOutput;
        //GenerateFroxels();
        if (_froxelsCamRelative == null || _froxelsCamRelative.Length == 0)
        {
            GenerateFroxelsInCameraSpace();
        }
    }
    
    public void SetUpShadow(int froxelCount)
    {
        points4 = new Vector4[froxelCount];
        for (var i = 0; i < points4.Length; i++)
        {
            points4[i] = new Vector4(0,0,0,1);
        }
    }

    public void SetUpOrient()
    {
        CleanUpOrient();
        ff = new FroxelFlat[_froxels.Length];
        cbOrientInput = new ComputeBuffer(ff.Length,sizeof(float)*3/*+sizeof(float)*3*8*/);
        
        cbOrientOutput = new ComputeBuffer(ff.Length,sizeof(float)*3);
        for (var i = 0; i < _froxels.Length; i++)
        {
            ff[i].center = _froxelsCamRelative[i].center;
//            ff[i].corner0 = _froxels[i].corners[0];
//            ff[i].corner1 = _froxels[i].corners[1];
//            ff[i].corner2 = _froxels[i].corners[2];
//            ff[i].corner3 = _froxels[i].corners[3];
//            ff[i].corner4 = _froxels[i].corners[4];
//            ff[i].corner5 = _froxels[i].corners[5];
//            ff[i].corner6 = _froxels[i].corners[6];
//            ff[i].corner7 = _froxels[i].corners[7];
        }
        cbOrientInput.SetData(ff);
        orientCompute.SetBuffer(0,"froxelsInput",cbOrientInput);
        orientCompute.SetBuffer(0,"froxelsOutput",cbOrientOutput);
    }

    public void CleanUpOrient()
    {
        cbOrientInput?.Dispose();
        cbOrientOutput?.Dispose();
    }
    
    void Update()
    {
        foreach (CustomPassVolume customPassVolume in cpvs)
        {
            customPassVolume.enabled = !disableResult;
        }
        DrawOutlineFrustrum();
        if (enableGenerateFroxelsEveryFrame)
        {
            //destroy old points
            foreach (GameObject g in pointsGos)
            {
                Destroy(g);
            }
            //GenerateFroxels();
            GenerateFroxelsInCameraSpace();
        }
        else
        {
            if (orientFroxels && !enableTransformedChilds)
            {
                //not necessary with transform in child
                OrientFroxelsFromCameraToWorldComputeShader();
            }
        }
        
        if (enableDebugDraw)
        {
            if (drawAll)
            {
                foreach (Froxel froxel in _froxels)
                {
                    DrawFrustum(froxel);
                }
            }
            else if (drawSingle)
            {
                DrawFrustum(_froxels[singleFroxel.z * (amount.x * amount.y) + singleFroxel.y * amount.x + singleFroxel.x]);
            }

            DrawDepth();
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

    public void GenerateFroxelsInCameraSpace()
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
        _froxelsCamRelative = new Froxel[amount.x*amount.y*amount.z];
        
        if(!useDepthdistribution2)
            GenCurveTexture(depthDistribution);
        else
        {
            GenCurveTexture(depthDistribution2);
        }
        SetUpShadow(_froxels.Length);
        
        Vector3[] fC = new Vector3[4];
        Vector3[] nC = new Vector3[4];
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, fC);
        _camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), _camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nC);
        //Debug.Log(String.Join("",fC.ToList().ConvertAll(i => i.ToString()).ToArray()));
        pointsCamRelative = new Vector3[(amount.x+1)*(amount.y+1)*(amount.z+1)];
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
                    
                    //point = _camera.transform.localToWorldMatrix.MultiplyPoint3x4(point);
                    pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + x] = point;
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
                        pointsCamRelative[(z+1) * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + x],
                        pointsCamRelative[(z+1) * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + (x+1)],
                        pointsCamRelative[(z+1) * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + x],
                        pointsCamRelative[(z+1) * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + (x+1)],
                        pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + x],
                        pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + (x+1)],
                        pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + x],
                        pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + (x+1)]
                    };
                    //Frustum f = GenerateFrustum(corners);
                    Froxel froxel = new Froxel();
                    froxel.corners = corners;
                    froxel.center = CalculateCenter(froxel);
                    //GameObject go =SpawnPointChild(froxel.center);
                    //froxel.goT = go.transform;
                    //pointsGos.Add(go);
                    _froxels[z * ((amount.x) * (amount.y)) + y * (amount.x) + x].corners = new Vector3[8];
                    _froxelsCamRelative[z * ((amount.x) * (amount.y)) + y * (amount.x) + x]= froxel;
                }
            }
        }
        SetUpOrient();
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
    
//    void OrientFroxels()
//    {
//        //Matrix4x4 inv = Matrix4x4.Inverse(lastFrameWorldToLocal);
//        Matrix4x4 comb = _camera.transform.localToWorldMatrix * lastFrameWorldToLocal;
//        for (var i = 0; i < _froxels.Length; i++)
//        {
//            for (int index = 0; index < _froxels[i].corners.Length; index++)
//            {
//                //froxel.corners[index] = lastFrameWorldToLocal.MultiplyPoint(froxel.corners[index]);
//                //froxel.corners[index] = _camera.transform.localToWorldMatrix.MultiplyPoint(froxel.corners[index]) ;
//                _froxels[i].corners[index] = comb.MultiplyPoint3x4(_froxels[i].corners[index]);
//            }
//
//            _froxels[i].center = comb.MultiplyPoint3x4(_froxels[i].center);
//        }
//
//        lastFrameWorldToLocal = _camera.transform.worldToLocalMatrix;
//    }

    void OrientFroxelsFromCameraToWorldComputeShader()
    {
        //var stopwatch = new Stopwatch();
        //stopwatch.Start();
        
        
//        for (var i = 0; i < _froxels.Length; i++)
//        {
//            //GetFlat(ref _froxels[i], ref ff[i]);
//            ff[i].center = _froxelsCamRelative[i].center;
////            ff[i].corner0 = _froxels[i].corners[0];
////            ff[i].corner1 = _froxels[i].corners[1];
////            ff[i].corner2 = _froxels[i].corners[2];
////            ff[i].corner3 = _froxels[i].corners[3];
////            ff[i].corner4 = _froxels[i].corners[4];
////            ff[i].corner5 = _froxels[i].corners[5];
////            ff[i].corner6 = _froxels[i].corners[6];
////            ff[i].corner7 = _froxels[i].corners[7];
//        }
//        //Debug.Log("in1: "+stopwatch.ElapsedMilliseconds);
//        cbOrientInput.SetData(ff);
//        //Debug.Log("in2: "+stopwatch.ElapsedMilliseconds);
//        
//        orientCompute.SetBuffer(0,"froxelsInput",cbOrientInput);
//        orientCompute.SetBuffer(0,"froxelsOutput",cbOrientOutput);
        Matrix4x4 comb = _camera.transform.localToWorldMatrix;// * lastFrameWorldToLocal;
        orientCompute.SetMatrix("comb",comb);
        
        orientCompute.Dispatch(0,ff.Length/1024,1,1);
        
        cbOrientOutput.GetData(ff);
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
            for (int index = 0; index < _froxelsCamRelative[debugFroxel].corners.Length; index++)
            {
                //froxel.corners[index] = lastFrameWorldToLocal.MultiplyPoint(froxel.corners[index]);
                //froxel.corners[index] = _camera.transform.localToWorldMatrix.MultiplyPoint(froxel.corners[index]) ;
                _froxels[debugFroxel].corners[index] = comb.MultiplyPoint3x4(_froxelsCamRelative[debugFroxel].corners[index]);
            }
        }
        
        //lastFrameWorldToLocal = _camera.transform.worldToLocalMatrix;
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
        from3DTo2D.SetTexture("_CurveTexture",curveTexture);
        //AdjustRenderTexture(scatteringResult2D, false, false);
        Graphics.Blit(null,scatteringResult2D,from3DTo2D);
    }

    void DrawFrustum(Froxel f)
    {
        //farLeftBottom, farRightBottom, farLeftTop, farRightTop, nearLeftBottom, nearRightBottom, nearLeftTop, nearRightTop
        if (drawEdges && !enableTransformedChilds)
        {
            Vis.DrawLine(f.corners[4], f.corners[0], frustrumThickness, Color.cyan, Style.Unlit);
            Vis.DrawLine(f.corners[5], f.corners[1], frustrumThickness, Color.cyan, Style.Unlit);
            Vis.DrawLine(f.corners[6], f.corners[2], frustrumThickness, Color.cyan, Style.Unlit);
            Vis.DrawLine(f.corners[7], f.corners[3], frustrumThickness, Color.cyan, Style.Unlit);

            Vis.DrawLine(f.corners[4], f.corners[5], frustrumThickness, Color.cyan, Style.Unlit);
            Vis.DrawLine(f.corners[6], f.corners[7], frustrumThickness, Color.cyan, Style.Unlit);
            Vis.DrawLine(f.corners[0], f.corners[1], frustrumThickness, Color.cyan, Style.Unlit);
            Vis.DrawLine(f.corners[2], f.corners[3], frustrumThickness, Color.cyan, Style.Unlit);

            Vis.DrawLine(f.corners[4], f.corners[6], frustrumThickness, Color.cyan, Style.Unlit);
            Vis.DrawLine(f.corners[5], f.corners[7], frustrumThickness, Color.cyan, Style.Unlit);
            Vis.DrawLine(f.corners[0], f.corners[2], frustrumThickness, Color.cyan, Style.Unlit);
            Vis.DrawLine(f.corners[1], f.corners[3], frustrumThickness, Color.cyan, Style.Unlit);
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
        float result;
        if(!useDepthdistribution2)
            result = depthDistribution.Evaluate(value);
        else
        {
            result = depthDistribution2.Evaluate(value);
        }
        return result;
    }
    
    void DrawOutlineFrustrum()
    {
//        Vector3[] nearCornersWorld = new Vector3[4];
//        Vector3[] farCornersWorld = new Vector3[4];
//        _camera.CalculateFrustumCorners(new Rect(0,0,1,1),_camera.nearClipPlane,Camera.MonoOrStereoscopicEye.Mono, nearCornersWorld);
//        _camera.CalculateFrustumCorners(new Rect(0,0,1,1),_camera.farClipPlane,Camera.MonoOrStereoscopicEye.Mono, farCornersWorld);
//        for (int i = 0; i < 4; i++)
//        {
//            Vector3 transformed1 = _camera.transform.localToWorldMatrix * nearCornersWorld[i];
//            Vector3 transformed2 = _camera.transform.localToWorldMatrix * farCornersWorld[i];
//            Debug.DrawLine(transformed1,transformed2,Color.black);
//        }

        Matrix4x4 comb = _camera.transform.localToWorldMatrix;            

        Vector3 bottomleftnear = pointsCamRelative[0 * ((amount.x+1) * (amount.y+1)) + 0 * (amount.x+1) + 0];//lbn
        Vector3 bottomleftfar = pointsCamRelative[amount.z * ((amount.x+1) * (amount.y+1)) + 0 * (amount.x+1) + 0];//lbf
        Vector3 topleftnear = pointsCamRelative[0 * ((amount.x+1) * (amount.y+1)) + (amount.y) * (amount.x+1) + 0];//ltn
        Vector3 topleftfar = pointsCamRelative[amount.z * ((amount.x+1) * (amount.y+1)) + (amount.y) * (amount.x+1) + 0];//ltf
        Vector3 bottomrightnear = pointsCamRelative[0 * ((amount.x+1) * (amount.y+1)) + 0 * (amount.x+1) + amount.x];//lbn
        Vector3 bottomrightfar = pointsCamRelative[amount.z * ((amount.x+1) * (amount.y+1)) + 0 * (amount.x+1) + amount.x];//lbf
        Vector3 toprightnear = pointsCamRelative[0 * ((amount.x+1) * (amount.y+1)) + (amount.y) * (amount.x+1) + (amount.x)];//ltn
        Vector3 toprightfar = pointsCamRelative[amount.z * ((amount.x+1) * (amount.y+1)) + (amount.y) * (amount.x+1) + (amount.x)];//ltf
        
        bottomleftnear = comb.MultiplyPoint3x4(bottomleftnear);
        bottomleftfar = comb.MultiplyPoint3x4(bottomleftfar);
        topleftnear = comb.MultiplyPoint3x4(topleftnear);
        topleftfar = comb.MultiplyPoint3x4(topleftfar);
        bottomrightnear = comb.MultiplyPoint3x4(bottomrightnear);
        bottomrightfar = comb.MultiplyPoint3x4(bottomrightfar);
        toprightnear = comb.MultiplyPoint3x4(toprightnear);
        toprightfar = comb.MultiplyPoint3x4(toprightfar);
        
        
        Debug.DrawLine(bottomleftnear,bottomleftfar,Color.black);
        Debug.DrawLine(topleftnear,topleftfar,Color.black);
        Debug.DrawLine(bottomrightnear,bottomrightfar,Color.black);
        Debug.DrawLine(toprightnear,toprightfar,Color.black);
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
        Vector3Int dispatches = new Vector3Int(CeilDispatch(size.x,32),CeilDispatch(size.y,32),1);
        scatteringCompute.Dispatch(0, dispatches.x,dispatches.y,dispatches.z);
        
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
        Vector3Int dispatches = new Vector3Int(CeilDispatch(densityBufferTexture.width,8),CeilDispatch(densityBufferTexture.height,8),CeilDispatch(densityBufferTexture.volumeDepth,16));
        densityCompute.Dispatch(0,dispatches.x,dispatches.y,dispatches.z);
        
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
        
        
        shadowManager.CalculateShadows(lightBufferTexture, points4, _camera.transform.position, enableTransformedChilds,singleFroxel.z * (amount.x * amount.y) + singleFroxel.y * amount.x + singleFroxel.x);
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
    
    //https://bitbucket.org/Unity-Technologies/cinematic-image-effects/src/96901525f6864a62aeadec288cc7749fef4c70a9/UnityProject/Assets/Standard%20Assets/Effects/CinematicEffects(BETA)/TonemappingColorGrading/TonemappingColorGrading.cs
    private void GenCurveTexture(AnimationCurve animationCurve)
    {
        //AnimationCurve master = colorGrading.curves.master;
        //AnimationCurve red = animationCurve;
        //AnimationCurve green = colorGrading.curves.green;
        //AnimationCurve blue = colorGrading.curves.blue;

        Color[] pixels = new Color[256];

        AnimationCurve inverseanimationCurve = new AnimationCurve();
//        for (int i = 0; i < animationCurve.length; i++)
//        {
//            Keyframe inverseKey = new Keyframe(animationCurve.keys[i].value, animationCurve.keys[i].time);
//            inverseanimationCurve.AddKey(inverseKey);
//        }
        int length =amount.z*2;
        for (int i = 0; i < length; i++)
        {
            float part = i*(1 / (float) length);
            Keyframe key = new Keyframe(part,animationCurve.Evaluate(part));
            //Keyframe inverseKey = new Keyframe(animationCurve.keys[i].value, animationCurve.keys[i].time);
            Keyframe inverseKey = new Keyframe(key.value,key.time);
            inverseanimationCurve.AddKey(inverseKey);
        }
        
        
        for (float i = 0f; i <= 1f; i += 1f / 255f)
        {
            //float m = Mathf.Clamp(master.Evaluate(i), 0f, 1f);
            float r = Mathf.Clamp(inverseanimationCurve.Evaluate(i), 0f, 1f);
            //float g = Mathf.Clamp(green.Evaluate(i), 0f, 1f);
            //float b = Mathf.Clamp(blue.Evaluate(i), 0f, 1f);
            pixels[(int)Mathf.Floor(i * 255f)] = new Color(r, 0, 0, 1);
        }
    
        curveTexture.SetPixels(pixels);
        curveTexture.Apply();
        Graphics.Blit(curveTexture,CurveOutput);
    }

    void DrawDepth()
    {
        //lbf, rbf, ltf, rtf, lbn, rbn, ltn, rtn,
//        Vector3[] corners = new Vector3[]
//        {
//      lbf      pointsCamRelative[(z+1) * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + x],
//      rbf      pointsCamRelative[(z+1) * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + (x+1)],
//      ltf      pointsCamRelative[(z+1) * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + x],
//      rtf      pointsCamRelative[(z+1) * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + (x+1)],
//      lbn      pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + x],
//      rbn      pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + (x+1)],
//      ltn      pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + x],
//      rtn      pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + (y+1) * (amount.x+1) + (x+1)]
//        };
        
        for (int z = 0; z < amount.z+1; z++)
        {
            //left to right vertical lines
//            for (int x = 0; x < amount.x+1; x++)
//            {
//                Vector3 bottom = pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + 0 * (amount.x+1) + x];//lbn
//                Vector3 top = pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + (amount.y) * (amount.x+1) + x];//ltn
//                Debug.DrawLine(bottom,top,Color.blue);
//            }
//            //bottom to top horizontal lines
//            for (int y = 0; y < amount.y+1; y++)
//            {
//                Vector3 left = pointsCamRelative[z * ((amount.x + 1) * (amount.y + 1)) + y * (amount.x + 1) + 0]; //lbn
//                Vector3 right = pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + y * (amount.x+1) + amount.x];//rbn
//                Debug.DrawLine(left,right,Color.red);
//            }
            Matrix4x4 comb = _camera.transform.localToWorldMatrix;            

            Vector3 bottomleft = pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + 0 * (amount.x+1) + 0];//lbn
            Vector3 topleft = pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + (amount.y) * (amount.x+1) + 0];//ltn
            Vector3 bottomright = pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + 0 * (amount.x+1) + amount.x];//lbn
            Vector3 topright = pointsCamRelative[z * ((amount.x+1) * (amount.y+1)) + (amount.y) * (amount.x+1) + (amount.x)];//ltn
            bottomleft = comb.MultiplyPoint3x4(bottomleft);
            topleft = comb.MultiplyPoint3x4(topleft);
            bottomright = comb.MultiplyPoint3x4(bottomright);
            topright = comb.MultiplyPoint3x4(topright);
            Debug.DrawLine(bottomleft,topleft,Color.cyan);
            Debug.DrawLine(topright,bottomright,Color.cyan);
            Debug.DrawLine(bottomleft,bottomright,Color.cyan);
            Debug.DrawLine(topright,topleft,Color.cyan);
        }
        
    }

    public static int CeilDispatch(int texPixels, int threads)
    {
        return Mathf.CeilToInt(texPixels / (float) threads);
    }
    public static int CeilDispatch(float texPixels, int threads)
    {
        return Mathf.CeilToInt(texPixels / (float) threads);
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
