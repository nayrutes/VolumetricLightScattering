using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShadowManager : MonoBehaviour
{
    [SerializeField] private LightCamera lightCamera;

    [SerializeField] private ComputeShader lightAccumulation;

    public Vector3 DebugPoint;
    [Range(0,1.0f)]
    public float DebugDepth;

    private Vector4[] points4;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetUp(int froxelCount)
    {
        points4 = new Vector4[froxelCount];
        for (var i = 0; i < points4.Length; i++)
        {
            points4[i] = new Vector4(0,0,0,1);
        }
    }
    
    public void CalculateShadows(RenderTexture current, RenderTexture withShadows, Froxels.Froxel[] froxel, bool enableTransformedChilds)
    {
        Matrix4x4 scaleMatrix = Matrix4x4.identity;
        scaleMatrix.m22 = -1.0f;
        //Matrix4x4 v = scaleMatrix * lightCamera.transform.worldToLocalMatrix;
        //Matrix4x4 vp = lightCamera.cam.projectionMatrix * v;
        Matrix4x4 vp;
        
        vp = lightCamera.cam.projectionMatrix * scaleMatrix * lightCamera.cam.transform.worldToLocalMatrix;
        int kernelId = lightAccumulation.FindKernel("CSMain");
        lightAccumulation.SetTexture( kernelId, "Input", current);
        lightAccumulation.SetTexture( kernelId, "Result", withShadows);
        lightAccumulation.SetTexture( kernelId, "LightDepth", lightCamera.depthTexture);
        lightAccumulation.SetMatrix("vp",vp);
        lightAccumulation.SetMatrix("convertTo01",CreateConvertionMatrixMinus11To01());
        lightAccumulation.SetVector("Input_TexelSize",new Vector4(current.width,current.height,current.volumeDepth,0));
        
        
        //var watch = System.Diagnostics.Stopwatch.StartNew();
        
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
        
//        watch.Stop();
//        var elapsedMs = watch.ElapsedMilliseconds;
//        Debug.Log(elapsedMs);

        //lightAccumulation.SetVectorArray("points", points4);
        ComputeBuffer cb = new ComputeBuffer(points4.Length,sizeof(float)*4);
        cb.SetData(points4);
        lightAccumulation.SetBuffer(kernelId,"points",cb);
        lightAccumulation.Dispatch(kernelId,current.height/8,current.width/8,current.volumeDepth/16);
        
        cb.Dispose();
        

        ProjectionTest.DrawDebugCube(lightCamera.transform,true);
        bool isInside;
        Vector3 projectedDebugLocal = ProjectionTest.WorldToProjectedLocal(DebugPoint, lightCamera.cam, true, out isInside);
        Vector3 projectedDebugWorld = ProjectionTest.ToWorld(projectedDebugLocal, lightCamera.cam.transform);
        Froxels.DrawPointCross(projectedDebugWorld,0.3f,isInside ? Color.white : Color.red);
        Froxels.DrawPointCross(DebugPoint,0.3f,Color.blue);
    }
    
    //shifts a point from [-1, 1] unit cube into [0, 1] unit cube 
    private Matrix4x4 CreateConvertionMatrixMinus11To01()
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

public static class Extensions
{
    public static Vector4 ToVector4(this Vector3 v)
    {
        return new Vector4(v.x,v.y,v.z,1);
    }
    
    public static void ToVector4(this Vector3 v, ref Vector4 v4)
    {
        v4.x = v.x;
        v4.y = v.y;
        v4.z = v.z;
        v4.w = 1;
    }
}
