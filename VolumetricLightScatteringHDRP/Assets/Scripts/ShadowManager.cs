using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShadowManager : MonoBehaviour
{
    [SerializeField] private LightCamera lightCamera;

    [SerializeField] private ComputeShader lightAccumulation;

    private bool debug = false;
    public Vector3 DebugPoint;
    [Range(0,1.0f)]
    public float DebugDepth;

    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
    
    public void CalculateShadows(RenderTexture lightBufferTexture,Vector4[] points4 , bool enableTransformedChilds, int debugIndex)
    {
        Matrix4x4 scaleMatrix = Matrix4x4.identity;
        scaleMatrix.m22 = -1.0f;
        //Matrix4x4 v = scaleMatrix * lightCamera.transform.worldToLocalMatrix;
        //Matrix4x4 vp = lightCamera.cam.projectionMatrix * v;
        Matrix4x4 vp;
        
        vp = lightCamera.cam.projectionMatrix * scaleMatrix * lightCamera.cam.transform.worldToLocalMatrix;
        int kernelId = lightAccumulation.FindKernel("CSMain");
        lightAccumulation.SetTexture( kernelId, "lightBufferTexture", lightBufferTexture);
        lightAccumulation.SetTexture( kernelId, "LightDepth", lightCamera.depthTexture);
        lightAccumulation.SetMatrix("vp",vp);
        lightAccumulation.SetMatrix("convertTo01",CreateConvertionMatrixMinus11To01());
        lightAccumulation.SetVector("Input_TexelSize",new Vector4(lightBufferTexture.width,lightBufferTexture.height,lightBufferTexture.volumeDepth,0));
        lightAccumulation.SetVector("lightWS",lightCamera.cam.transform.position);
        
        //var watch = System.Diagnostics.Stopwatch.StartNew();
        //
        
        
//        watch.Stop();
//        var elapsedMs = watch.ElapsedMilliseconds;
//        Debug.Log(elapsedMs);

        //lightAccumulation.SetVectorArray("points", points4);
        ComputeBuffer cb = new ComputeBuffer(points4.Length,sizeof(float)*4);
        cb.SetData(points4);
        lightAccumulation.SetBuffer(kernelId,"points",cb);
        Vector3Int dispatches = new Vector3Int(Froxels.CeilDispatch(lightBufferTexture.width,8),Froxels.CeilDispatch(lightBufferTexture.height,8),Froxels.CeilDispatch(lightBufferTexture.volumeDepth,16));
        lightAccumulation.Dispatch(kernelId,dispatches.x,dispatches.y,dispatches.z);
        
        cb.Dispose();

        if (debug)
        {
            ProjectionTest.DrawDebugCube(lightCamera.transform,true, Color.white);
            bool isInside;
            Vector3 projectedDebugLocal = ProjectionTest.WorldToProjectedLocal(DebugPoint, lightCamera.cam, true, out isInside);
            Vector3 projectedDebugWorld = ProjectionTest.ToWorld(projectedDebugLocal, lightCamera.cam.transform);
            Froxels.DrawPointCross(projectedDebugWorld,0.3f,isInside ? Color.white : Color.red);
            Froxels.DrawPointCross(DebugPoint,0.3f,Color.blue);

            Vector3 lightWS = lightCamera.cam.transform.position;
            Froxels.DrawPointCross(lightWS,1.0f,Color.green);
            int index = debugIndex;
            Vector3 pointWS = new Vector3(points4[index].x,points4[index].y,points4[index].z);
            Froxels.DrawPointCross(pointWS,1.0f,Color.red);
            Vector3 pointToLightRay = lightWS-pointWS;
            float pointToLightDistance = Vector3.Distance(lightWS,pointWS);
            float weight =  Mathf.Exp(-(pointToLightDistance));
            Debug.DrawLine(lightWS,pointWS,Color.green);
        }
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
