using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShadowManager : MonoBehaviour
{
    [SerializeField] private LightCamera lightCamera;

    [SerializeField] private ComputeShader lightAccumulation;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CalculateShadows(RenderTexture current, RenderTexture withShadows, Vector3[] points)
    {
        Matrix4x4 scaleMatrix = Matrix4x4.identity;
        scaleMatrix.m22 = -1.0f;
        //Matrix4x4 v = scaleMatrix * lightCamera.transform.worldToLocalMatrix;
        //Matrix4x4 vp = lightCamera.cam.projectionMatrix * v;
        Matrix4x4 vp = lightCamera.cam.projectionMatrix * scaleMatrix * lightCamera.transform.worldToLocalMatrix;
        
        lightAccumulation.SetTexture( 0, "Input", current);
        lightAccumulation.SetTexture( 0, "Result", withShadows);
        lightAccumulation.SetTexture( 0, "LightDepth", lightCamera.depthTexture);
        lightAccumulation.SetMatrix("vp",vp);
        lightAccumulation.SetMatrix("convertTo01",createConvertionMatrixMinus11To01());
        Vector4[] points4 = new Vector4[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            points4[i] = points[i].ToVector4();
        }

        //lightAccumulation.SetVectorArray("points", points4);
        ComputeBuffer cb = new ComputeBuffer(points.Length,sizeof(float)*4,ComputeBufferType.Constant);
        lightAccumulation.SetBuffer(0,"points",cb);
        lightAccumulation.Dispatch(0,current.height/8,current.width/8,current.volumeDepth/16);
        cb.Dispose();
    }
    
    //shifts a point from [-1, 1] unit cube into [0, 1] unit cube 
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

public static class Extensions
{
    public static Vector4 ToVector4(this Vector3 v)
    {
        return new Vector4(v.x,v.y,v.z,1);
    }
    
}
