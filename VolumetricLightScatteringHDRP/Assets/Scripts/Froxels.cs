using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private RenderTexture scatteringInputWithShadows;
    [SerializeField] private RenderTexture scatteringOutput;
    //[SerializeField] private Material renderMaterial;
    [SerializeField] private Material fullScreenPassHdrpMaterial;
    [SerializeField] private Vector4 insetValue;
    [SerializeField] private ShadowManager shadowManager;
    
    [Header("inX, inY, inZ")]
    [SerializeField] private Vector3Int amount;

    public bool enableDebugDraw = true;
    public bool drawCorners = true;
    public bool drawEdges = true;
    public bool toggleSingleAll = false;
    public Vector3Int singleFroxel;
    public bool enableScatteringCompute = true;
    public bool enableGenerateFroxelsEveryFrame = true;
    public bool enableCalculateShadows = true;
    
    [SerializeField] private AnimationCurve depthDistribution;
    
    private Camera _camera;
    private Frustum[] _froxels;

    private DebugSlice _debugSlice;
    private Vector3 froxelLastPositionOrigin;
    private Matrix4x4 lastFrameWorldToLocal;
    struct Froxel
    {
        public Frustum frustum;
    }
    
    void Start()
    {
        _camera = GetComponent<Camera>();
        _debugSlice = FindObjectOfType<DebugSlice>();
       // _debugSlice.texture3DToSlice = scatteringOutput;
        //GenerateFroxels();
        GenerateFroxelsInWorldSpace();
    }
    
    void Update()
    {
        DrawOutlineFrustrum();
        if(enableGenerateFroxelsEveryFrame)
            //GenerateFroxels();
            GenerateFroxelsInWorldSpace();
        else
            OrientFroxels();
        
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

        if (enableCalculateShadows)
        {
            CalculateShadows();
        }
        else
        {
            scatteringInputWithShadows = scatteringInput;
        }
        if(enableScatteringCompute)
            RunScatteringCompute();
        
        
        
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
        lastFrameWorldToLocal = _camera.transform.worldToLocalMatrix;
        _froxels = new Frustum[amount.x*amount.y*amount.z];
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
                    float zl = Mathf.InverseLerp(0, amount.z, z);
                    zl = CreateNonLinear(zl);
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
                    Frustum f = GenerateFrustum(corners);
                    _froxels[z * ((amount.x) * (amount.y)) + y * (amount.x) + x] = f;
                }
            }
        }
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
    private void GenerateFroxels()
    {
        froxelLastPositionOrigin = _camera.transform.position;
        lastFrameWorldToLocal = _camera.transform.worldToLocalMatrix;
        
        _froxels = new Frustum[amount.x*amount.y*amount.z];
        
        //Froxelize Unitcube
        for (int k = 0; k < amount.z; k++)
        {
            float inZFar = ((1.0f / amount.z) * k);
            float inZNear = ((1.0f / amount.z) * (k+1));
            
            //reverse projection non-linear depth before applying projMatrix
            float f = _camera.farClipPlane;
            float n = _camera.nearClipPlane;
            float ratio = f / n;

//            inZNear = 1-Mathf.Pow((1 - inZNear),2);
//            inZFar = 1-Mathf.Pow((1 - inZFar),2);

            //inZNear = 1-Mathf.Pow((inZNear),2);
            //inZFar = 1-Mathf.Pow((inZFar),2);

//            inZNear = -inZNear;
//            inZFar = -inZFar;
            //if(inZNear>0)
                inZNear = (1-1 / (inZNear));
            //else
            //{
            //    inZNear = 1 - inZNear;
            //}
            //if(inZFar>0)
                inZFar = (1-1 / (inZFar));

                //inZNear = ((1 / (inZNear - 2)) + 1);
                //inZFar = ((1 / (inZFar - 2)) + 1);
            //else
            //{
            //    inZFar = 1 - inZFar;
            //}

            //inZNear = ((f-n) / (n*f) * (inZNear) - (1 / n));
            //inZFar = ((n-f) / (n*f) * (inZFar) - (1 / n));
            
            //inZNear = (n-n*inZNear)/(f*inZNear - n*inZNear);
            //inZFar = (n-n*inZFar)/(f*inZFar - n*inZFar);

            //inZNear = 1 / (1 - inZNear);
            //inZFar = 1 / (1 - inZFar);
            
            inZFar = ConvertRange01ToMinus11(inZFar);
            inZNear = ConvertRange01ToMinus11(inZNear);
            
            //inZNear = (2-1 / (inZNear));
            //inZFar = (2-1 / (inZFar));
            
//            inZNear = -inZNear;
//            inZFar = -inZFar;
            
            //inZNear = 1 / inZNear;
            //inZFar = 1 / inZFar;
            
            //inZNear = (f + n) / (f - n) + (2 * n * f) / ((f - n) * inZNear);
            //inZFar = (f + n) / (f - n) + (2 * n * f) / ((f - n) * inZFar);
            
            //inZNear = 1/(-((f + n) / (f - n)) * inZNear - ((2 * f * n) / (f - n)) / -inZNear);
            //inZFar = 1/(-((f + n) / (f - n)) * inZFar - ((2 * f * n) / (f - n)) / -inZFar);
            
            //inZNear = ((n-f) / (n*f) * inZNear - (1 / n));
            //inZFar = ((n-f) / (n*f) * inZFar - (1 / n));

            //close
            //inZNear = 1/((n - f*inZNear)/(inZNear*(n-f)));
            //inZFar = 1/((n - f*inZFar)/(inZFar*(n-f)));

            
            
            //inZNear = ((n-f) / (n) * inZNear + (f / n));
            //inZFar = ((n-f) / (n) * inZFar + (f / n));
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
                    corners[0] = (forward * inZFar + right*inXFractionLeft + up* inYFractionBottom);
                    corners[1] = (forward * inZFar + right*inXFractionRight + up* inYFractionBottom);
                    corners[2] = (forward * inZFar + right*inXFractionLeft + up* inYFractionTop);
                    corners[3] = (forward * inZFar + right*inXFractionRight + up* inYFractionTop);
                    corners[4] = (forward * inZNear + right*inXFractionLeft + up* inYFractionBottom);
                    corners[5] = (forward * inZNear + right*inXFractionRight + up* inYFractionBottom);
                    corners[6] = (forward * inZNear + right*inXFractionLeft + up* inYFractionTop);
                    corners[7] = (forward * inZNear + right*inXFractionRight + up* inYFractionTop);

//                    corners[0] = (forward * inZFar);
//                    corners[1] = (forward * inZFar);
//                    corners[2] = (forward * inZFar);
//                    corners[3] = (forward * inZFar + right*inXFractionRight + up* inYFractionTop);
//                    corners[4] = (forward * inZNear + right*inXFractionLeft + up* inYFractionBottom);
//                    corners[5] = (forward * inZNear + right*inXFractionRight + up* inYFractionBottom);
//                    corners[6] = (forward * inZNear + right*inXFractionLeft + up* inYFractionTop);
//                    corners[7] = (forward * inZNear + right*inXFractionRight + up* inYFractionTop);
                    
//                    for (int index = 0; index < corners.Length; index++)
//                    {
//                        //corners[index].z = -_camera.worldToCameraMatrix.MultiplyPoint3x4(corners[index]).z;
//                        corners[index].z *= -1;
////                        corners[index].z =
////                            (1 / (((_camera.farClipPlane - _camera.nearClipPlane) /
////                                   (_camera.nearClipPlane * _camera.farClipPlane)) * corners[index].z -
////                                  (1 / _camera.nearClipPlane))) * _camera.farClipPlane;
//                        corners[index] = _camera.transform.localToWorldMatrix.MultiplyPoint3x4(corners[index]);
//                    }

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
                    
                    //OrientFroxels();
                }
            }
        }
        //OrientFroxels();
        ProjectUnitCubeToFrustrum();
    }

    void ProjectUnitCubeToFrustrum()
    {
        //TODO check if better use Non-Jittered Projection matrix https://docs.unity3d.com/ScriptReference/Camera-nonJitteredProjectionMatrix.html
        Matrix4x4 invViewProjMatrix = _camera.projectionMatrix.inverse;
        var projectionMatrix = _camera.projectionMatrix;
        var m = _camera.worldToCameraMatrix;
        Matrix4x4 customProjMatrix = new Matrix4x4();//s = projectionMatrix;
        customProjMatrix.m00 = projectionMatrix.m00;
        customProjMatrix.m11 = projectionMatrix.m11;
        customProjMatrix.m32 = projectionMatrix.m32;
        //customProjMatrix.m33 = projectionMatrix.m33;
        customProjMatrix.m23 = projectionMatrix.m23;
        //customProjMatrix.
        foreach (Frustum froxel in _froxels)
        {
            float f = _camera.farClipPlane;
            float n = _camera.nearClipPlane;
            float nTmp = n;
            
            for(int index=0; index<froxel.corners.Length;index++)
            {
                Vector3 p = froxel.corners[index];
                //p = new Vector3(p.x,p.y,1-p.z);
                //froxel.corners[index] = invViewProjMatrix.MultiplyPoint(p);
                froxel.corners[index] = projectionMatrix.inverse.MultiplyPoint(p);
//                if(index>=4)
//                    froxel.corners[index] *= -1;
//                if(index==0)
//                    nTmp = froxel.corners[0].z;
                froxel.corners[index].z *= -1;
                //froxel.corners[index] *= ((f-n) / (n*f) * (1 -froxel.corners[index].z) - (1 / n));
                //froxel.corners[index] *= (n-n*froxel.corners[index].z)/(f*froxel.corners[index].z - n*froxel.corners[index].z);
                //froxel.corners[index] *= -1;
                //froxel.corners[index] = ((froxel.corners[index] + new Vector3(0, 0, n))) * f / n;// + new Vector3(0,0,f);
                //scaling and back right, only front? small offset
                //froxel.corners[index] = (froxel.corners[index]) * (f / n);// - froxel.corners[index] * (f-n)/(f+n);
                //not working correctly
                //froxel.corners[index] = new Vector3(0,0,n)+ (f - n) / n * froxel.corners[index];
                //deviding shift into seperate steps
                float a = 0;
                float b = n;
                float c = n;
                float d = f;
                
//                froxel.corners[index].z = froxel.corners[index].z - new Vector3(0, 0, a).z;
//                froxel.corners[index].z = froxel.corners[index].z / (b-a);
////                //changing scaling?
//                froxel.corners[index].z = froxel.corners[index].z *(d-c);
//                froxel.corners[index].z = froxel.corners[index].z+new Vector3(0,0,c).z;
                
                //froxel.corners[index].z = (1 / (((f - n) /(n * f)) * froxel.corners[index].z -(1 / n)));
                
                //froxel.corners[index] = (froxel.corners[index] * f / n)- (new Vector3(0, 0, n)*n/f);
                
                //froxel.corners[index] -= new Vector3(0,0,-1);
                //froxel.corners[index] /= -(f + n) / (f - n);
                //froxel.corners[index] -= new Vector3(0,0,-(2 * f * n) / (f - n));
                froxel.corners[index] = _camera.transform.localToWorldMatrix.MultiplyPoint3x4(froxel.corners[index]);
            }
        }
    }
    
    void OrientFroxels()
    {
        //Matrix4x4 inv = Matrix4x4.Inverse(lastFrameWorldToLocal);
        Matrix4x4 comb = _camera.transform.localToWorldMatrix * lastFrameWorldToLocal;
        foreach (Frustum froxel in _froxels)
        {
            for(int index=0; index<froxel.corners.Length;index++)
            {
                //froxel.corners[index] = lastFrameWorldToLocal.MultiplyPoint(froxel.corners[index]);
                //froxel.corners[index] = _camera.transform.localToWorldMatrix.MultiplyPoint(froxel.corners[index]) ;
                froxel.corners[index] = comb.MultiplyPoint3x4(froxel.corners[index]);
            }
        }

        lastFrameWorldToLocal = _camera.transform.worldToLocalMatrix;
    }

    void AdjustFroxelsPosition()
    {
        Vector3 moved = _camera.transform.position - froxelLastPositionOrigin;
        froxelLastPositionOrigin = _camera.transform.position;
        foreach (Frustum froxel in _froxels)
        {
            for(int index=0; index<froxel.corners.Length;index++)
            {
                //froxel.corners[index] += moved;
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
        
//        RenderTexture textureInput = new RenderTexture(amount.x,amount.y,0,RenderTextureFormat.ARGBFloat,0);
//        textureInput.dimension = TextureDimension.Tex3D;
//        textureInput.volumeDepth = amount.z;
//        //textureInput.filterMode = 
//        textureInput.enableRandomWrite = true;
//        RenderTexture textureOutput = new RenderTexture(amount.x,amount.y,0,RenderTextureFormat.ARGBFloat,0);
//        textureOutput.dimension = TextureDimension.Tex3D;
//        textureOutput.volumeDepth = amount.z;
//        textureOutput.enableRandomWrite = true;


//        scatteringInput.Release();
//        scatteringOutput.Release();
//        scatteringInput.width = scatteringOutput.width = amount.x;
//        scatteringInput.height = scatteringOutput.height = amount.y;
//        scatteringInput.volumeDepth = scatteringOutput.volumeDepth = amount.z;
//        scatteringInput.enableRandomWrite = true;
//        scatteringOutput.enableRandomWrite = true;
//        scatteringInput.Create();
//        scatteringOutput.Create();
        //AdjustRenderTexture(scatteringInput);
        Vector4 size = new Vector4(scatteringInputWithShadows.width, scatteringInputWithShadows.height,
            scatteringInputWithShadows.volumeDepth, 0);
        
        AdjustRenderTexture(scatteringOutput);
        
        scatteringCompute.SetTexture(0, "Input",scatteringInputWithShadows);
        scatteringCompute.SetTexture(0, "Result",scatteringOutput);
        scatteringCompute.SetVector("insetValue",insetValue);
        scatteringCompute.SetVector("size", size);
        
        //int threadGroupsX = amount.z / 256;
        scatteringCompute.Dispatch(0, (int)(size.x/32.0f),(int)(size.y/32.0f),1);
        
        scatteringCompute.SetInt("VOLUME_DEPTH",amount.z);
    }

//    private void OnRenderImage(RenderTexture src, RenderTexture dest)
//    {
//        //renderMaterial.SetTexture();
//        Graphics.Blit(src,dest,renderMaterial);
//    }

    private void CalculateShadows()
    {
        //TODO prepare rendertextures
        AdjustRenderTexture(scatteringInputWithShadows);
        AdjustRenderTexture(scatteringInput);
        
        Vector3[] centers = new Vector3[_froxels.Length];
        for (var i = 0; i < _froxels.Length; i++)
        {
            centers[i] = CalculateCenter(_froxels[i]);
        }
        shadowManager.CalculateShadows(scatteringInput, scatteringInputWithShadows, centers);
    }


    public static void DrawPointCross (Vector3 centre, float radius, Color color) {
        Debug.DrawRay(new Vector3(centre.x-radius,centre.y,centre.z), new  Vector3(radius*2 ,0,0), color);
        Debug.DrawRay(new Vector3(centre.x,centre.y-radius,centre.z), new  Vector3(0 ,radius*2,0), color);
        Debug.DrawRay(new Vector3(centre.x,centre.y,centre.z-radius), new  Vector3(0 ,0,radius*2), color);
    }

    public Vector3 CalculateCenter(Frustum f)
    {
        Vector3 result = new Vector3(0,0,0);
        foreach (Vector3 corner in f.corners)
        {
            result += corner;
        }

        result /= 8;
        return result;
    }

    private void AdjustRenderTexture(RenderTexture r)
    {
        r.Release();
        r.width = amount.x;
        r.height = amount.y;
        r.volumeDepth = amount.z;
        r.enableRandomWrite = true;
        r.Create();
    }
}
